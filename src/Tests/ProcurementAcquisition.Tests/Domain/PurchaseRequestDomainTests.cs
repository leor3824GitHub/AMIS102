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
    public void Submit_WhenDraft_ChangesStatusToSubmitted()
    {
        var pr = CreatePr();

        pr.Submit();

        pr.Status.ShouldBe(PurchaseRequestStatus.Submitted);
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
            null, Guid.NewGuid(), null, null, null, null,
            []);

        act.ShouldThrow<InvalidOperationException>();
    }

    private static PurchaseRequest CreatePr(decimal quantity = 2, decimal unitCost = 500m) =>
        PurchaseRequest.Create(
            tenantId: "tenant-1",
            prNumber: "PR-2025-001",
            departmentId: Guid.NewGuid(),
            section: null,
            purpose: "Purchase of office supplies",
            prType: PrType.Planned,
            justification: null,
            requestedById: Guid.NewGuid(),
            saiNumber: null,
            saiDate: null,
            alobsNumber: null,
            alobsDate: null,
            lineItems: [(quantity, "piece", "Bond Paper A4", unitCost)]);
}

