using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Domain;

public sealed class PurchaseRequestDomainTests
{
    [Fact]
    public void Create_ValidInput_CreatesDraftRequest()
    {
        var pr = CreatePr();

        pr.Id.ShouldNotBe(Guid.Empty);
        pr.Status.ShouldBe(PurchaseRequestStatus.Draft);
        pr.LineItems.ShouldNotBeEmpty();
    }

    [Fact]
    public void Create_LineItemsTotalCost_IsCalculatedCorrectly()
    {
        var pr = CreatePr(quantity: 3, unitCost: 1000m);

        pr.LineItems[0].EstimatedTotalCost.ShouldBe(3000m);
    }

    [Fact]
    public void Submit_WhenDraft_ChangesStatusToPendingFundsAvailable()
    {
        var pr = CreatePr();

        pr.Submit();

        pr.Status.ShouldBe(PurchaseRequestStatus.PendingFundsAvailable);
    }

    [Fact]
    public void Submit_WhenNotDraft_Throws()
    {
        var pr = CreatePr();
        pr.Submit();

        var act = pr.Submit;

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Update_WhenNotDraft_Throws()
    {
        var pr = CreatePr();
        pr.Submit();

        var act = () => pr.Update(
            Guid.NewGuid(), null, "Updated purpose", PrType.Planned,
            null, "Test User", null, null, null, null,
            []);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void CertifyFundsAvailable_AfterSubmit_AssignsUacsAndMovesToPendingApproval()
    {
        var pr = CreatePr();
        pr.Submit();
        var uacs = pr.LineItems.ToDictionary(li => li.ItemNo, _ => "1-07-05-030");

        pr.CertifyFundsAvailable(Guid.NewGuid(), "Jane Accountant", uacs, "ALOBS-2026-001", DateOnly.FromDateTime(DateTime.UtcNow));

        pr.Status.ShouldBe(PurchaseRequestStatus.PendingApproval);
        pr.FundsAvailableCertifiedByName.ShouldBe("Jane Accountant");
        pr.FundsAvailableCertifiedOnUtc.ShouldNotBeNull();
        pr.AlobsNumber.ShouldBe("ALOBS-2026-001");
        pr.LineItems[0].UacsObjectCode.ShouldBe("1-07-05-030");
    }

    [Fact]
    public void CertifyFundsAvailable_WhenNotPendingFundsAvailable_Throws()
    {
        var pr = CreatePr();

        var act = () => pr.CertifyFundsAvailable(
            Guid.NewGuid(), "Jane", new Dictionary<int, string> { [1] = "1-07-05-030" }, null, null);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void CertifyFundsAvailable_WithMissingUacsForALine_Throws()
    {
        var pr = CreatePr();
        pr.Submit();
        var uacs = new Dictionary<int, string>(); // empty — line 1 is missing

        var act = () => pr.CertifyFundsAvailable(Guid.NewGuid(), "Jane", uacs, null, null);

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Approve_AfterCertifyFundsAvailable_MovesToApproved()
    {
        var pr = CreatePr();
        pr.Submit();
        pr.CertifyFundsAvailable(Guid.NewGuid(), "Jane Accountant",
            pr.LineItems.ToDictionary(li => li.ItemNo, _ => "1-07-05-030"), null, null);

        pr.Approve("John HoPE", Guid.NewGuid());

        pr.Status.ShouldBe(PurchaseRequestStatus.Approved);
        pr.ApprovedByName.ShouldBe("John HoPE");
        pr.ApprovedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Approve_WhenPendingFundsAvailable_Throws()
    {
        var pr = CreatePr();
        pr.Submit(); // PendingFundsAvailable — HoPE cannot approve yet

        var act = () => pr.Approve("John HoPE");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void ReturnForRevision_FromPendingFundsAvailable_RevertsToDraftWithReason()
    {
        var pr = CreatePr();
        pr.Submit();

        pr.ReturnForRevision(Guid.NewGuid(), "Jane Accountant", "Wrong department charged.");

        pr.Status.ShouldBe(PurchaseRequestStatus.Draft);
        pr.ReturnedReason.ShouldBe("Wrong department charged.");
        pr.ReturnedByName.ShouldBe("Jane Accountant");
        pr.ReturnedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public void ReturnForRevision_FromPendingApproval_AlsoReverts()
    {
        var pr = CreatePr();
        pr.Submit();
        pr.CertifyFundsAvailable(Guid.NewGuid(), "Jane",
            pr.LineItems.ToDictionary(li => li.ItemNo, _ => "1-07-05-030"), null, null);

        pr.ReturnForRevision(Guid.NewGuid(), "John HoPE", "Procurement mode incorrect.");

        pr.Status.ShouldBe(PurchaseRequestStatus.Draft);
        pr.ReturnedByName.ShouldBe("John HoPE");
    }

    [Fact]
    public void ReturnForRevision_FromDraft_Throws()
    {
        var pr = CreatePr();

        var act = () => pr.ReturnForRevision(Guid.NewGuid(), "Jane", "should not be possible");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Submit_AfterReturnForRevision_ClearsReturnMetadata()
    {
        var pr = CreatePr();
        pr.Submit();
        pr.ReturnForRevision(Guid.NewGuid(), "Jane", "Fix line items.");
        pr.ReturnedReason.ShouldNotBeNull();

        pr.Submit();

        pr.Status.ShouldBe(PurchaseRequestStatus.PendingFundsAvailable);
        pr.ReturnedReason.ShouldBeNull();
        pr.ReturnedByName.ShouldBeNull();
        pr.ReturnedOnUtc.ShouldBeNull();
    }

    [Fact]
    public void Reject_WhenPendingApproval_MovesToRejected()
    {
        var pr = CreatePr();
        pr.Submit();
        pr.CertifyFundsAvailable(Guid.NewGuid(), "Jane",
            pr.LineItems.ToDictionary(li => li.ItemNo, _ => "1-07-05-030"), null, null);

        pr.Reject("Outside fiscal year.");

        pr.Status.ShouldBe(PurchaseRequestStatus.Rejected);
        pr.RejectionReason.ShouldBe("Outside fiscal year.");
    }

    [Fact]
    public void Reject_WhenPendingFundsAvailable_Throws()
    {
        var pr = CreatePr();
        pr.Submit();

        var act = () => pr.Reject("Should not reject yet — Accountant still reviewing.");

        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Create_LineItem_CatalogItemId_IsOptional()
    {
        var pr = CreatePr();
        pr.LineItems[0].CatalogItemId.ShouldBeNull();
    }

    [Fact]
    public void Create_LineItem_WithCatalogItemId_IsPersisted()
    {
        var catalogId = Guid.NewGuid();
        var pr = PurchaseRequest.Create(
            tenantId: "tenant-1",
            prNumber: "PR-2026-002",
            departmentId: Guid.NewGuid(),
            responsibilityCenterCode: null,
            purpose: "Purchase ICT equipment",
            prType: PrType.Planned,
            justification: null,
            requestedByName: "Test User",
            saiNumber: null,
            saiDate: null,
            alobsNumber: null,
            alobsDate: null,
            lineItems: [new PurchaseRequestLineItemData(1, "piece", "Desktop Computer", 25000m, catalogId)]);

        pr.LineItems[0].CatalogItemId.ShouldBe(catalogId);
    }

    [Fact]
    public void Update_LineItem_CanChangeCatalogItemId()
    {
        var pr = CreatePr();
        var newCatalogId = Guid.NewGuid();

        pr.Update(
            Guid.NewGuid(), null, "Updated purpose", PrType.Planned,
            null, "Test User", null, null, null, null,
            [new PurchaseRequestLineItemData(3, "piece", "Different item", 200m, newCatalogId)]);

        pr.LineItems[0].CatalogItemId.ShouldBe(newCatalogId);
    }

    [Fact]
    public void CertifyFundsAvailable_PreservesCatalogItemId()
    {
        var catalogId = Guid.NewGuid();
        var pr = PurchaseRequest.Create(
            tenantId: "tenant-1",
            prNumber: "PR-2026-003",
            departmentId: Guid.NewGuid(),
            responsibilityCenterCode: null,
            purpose: "Purchase ICT equipment",
            prType: PrType.Planned,
            justification: null,
            requestedByName: "Test User",
            saiNumber: null,
            saiDate: null,
            alobsNumber: null,
            alobsDate: null,
            lineItems: [new PurchaseRequestLineItemData(1, "piece", "Desktop Computer", 25000m, catalogId)]);
        pr.Submit();

        pr.CertifyFundsAvailable(
            Guid.NewGuid(), "Jane Accountant",
            pr.LineItems.ToDictionary(li => li.ItemNo, _ => "1-07-05-030"),
            null, null);

        pr.LineItems[0].CatalogItemId.ShouldBe(catalogId);
        pr.LineItems[0].UacsObjectCode.ShouldBe("1-07-05-030");
    }

    private static PurchaseRequest CreatePr(decimal quantity = 2, decimal unitCost = 500m) =>
        PurchaseRequest.Create(
            tenantId: "tenant-1",
            prNumber: "PR-2025-001",
            departmentId: Guid.NewGuid(),
            responsibilityCenterCode: null,
            purpose: "Purchase of office supplies",
            prType: PrType.Planned,
            justification: null,
            requestedByName: "Test User",
            saiNumber: null,
            saiDate: null,
            alobsNumber: null,
            alobsDate: null,
            lineItems: [new PurchaseRequestLineItemData(quantity, "piece", "Bond Paper A4", unitCost)]);
}
