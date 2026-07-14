using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SearchPurchaseRequests;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Integration;

/// <summary>
/// Guards the Job Order "Link Purchase Request" picker: a PR whose goods have already been received must not be
/// offered as the origin of a new JO. "Received" means some purchase order raised against the PR carries a
/// non-cancelled Inspection &amp; Acceptance Report — IARs hang off a PO, never off a PR or a JO directly, so
/// <see cref="SearchPurchaseRequestsQuery.ExcludeWithIar"/> has to walk PR → PO → IAR.
/// </summary>
public sealed class SearchPurchaseRequestsExcludeWithIarTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_ExcludeWithIar_DropsOnlyPurchaseRequestsWhosePoHasANonCancelledIar()
    {
        using var ctx = new ProcurementTestContext(Tenant);

        var accepted = await SeedPurchaseRequestAsync(ctx.Db, "PR-ACCEPTED");
        await SeedPurchaseOrderWithIarAsync(ctx.Db, accepted.Id, "PO-1", IarOutcome.Accepted);

        var cancelledIar = await SeedPurchaseRequestAsync(ctx.Db, "PR-CANCELLED-IAR");
        await SeedPurchaseOrderWithIarAsync(ctx.Db, cancelledIar.Id, "PO-2", IarOutcome.Cancelled);

        var poOnly = await SeedPurchaseRequestAsync(ctx.Db, "PR-PO-NO-IAR");
        await SeedPurchaseOrderWithIarAsync(ctx.Db, poOnly.Id, "PO-3", IarOutcome.None);

        await SeedPurchaseRequestAsync(ctx.Db, "PR-BARE");

        var handler = new SearchPurchaseRequestsQueryHandler(ctx.Db);

        var filtered = await handler.Handle(
            new SearchPurchaseRequestsQuery { ExcludeWithIar = true }, CancellationToken.None);

        // Only the received PR drops off. A cancelled IAR does not count as received, a PO without an IAR is still
        // open, and a PR that never reached PO stage was never in question.
        filtered.Items.Select(x => x.PrNumber)
            .ShouldBe(["PR-CANCELLED-IAR", "PR-PO-NO-IAR", "PR-BARE"], ignoreOrder: true);
        filtered.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithoutExcludeWithIar_ReturnsEveryPurchaseRequest()
    {
        using var ctx = new ProcurementTestContext(Tenant);

        var received = await SeedPurchaseRequestAsync(ctx.Db, "PR-ACCEPTED");
        await SeedPurchaseOrderWithIarAsync(ctx.Db, received.Id, "PO-1", IarOutcome.Accepted);
        await SeedPurchaseRequestAsync(ctx.Db, "PR-BARE");

        var handler = new SearchPurchaseRequestsQueryHandler(ctx.Db);

        // The exclusion must be opt-in — every other PR consumer (the list page, the canvass picker) still needs
        // to see received PRs. If this ever returns 1, the filter has leaked into the default path.
        var unfiltered = await handler.Handle(new SearchPurchaseRequestsQuery(), CancellationToken.None);

        unfiltered.TotalCount.ShouldBe(2);
    }

    private enum IarOutcome
    {
        None,
        Accepted,
        Cancelled
    }

    private static async Task<PurchaseRequest> SeedPurchaseRequestAsync(ProcurementDbContext db, string prNumber)
    {
        var pr = PurchaseRequest.Create(
            Tenant, prNumber, departmentId: Guid.NewGuid(), responsibilityCenterCode: "RC-1",
            purpose: "Office use", prType: PrType.Planned, justification: null,
            requestedByName: "Requester", saiNumber: null, saiDate: null, alobsNumber: null, alobsDate: null,
            lineItems: [new PurchaseRequestLineItemData(1m, "unit", "Steel Cabinet", 5000m)]);

        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync();
        return pr;
    }

    private static async Task SeedPurchaseOrderWithIarAsync(
        ProcurementDbContext db, Guid purchaseRequestId, string poNumber, IarOutcome outcome)
    {
        var po = PurchaseOrder.Create(
            Tenant, poNumber, purchaseRequestId, canvassRequestId: null, supplierId: Guid.NewGuid(),
            supplierName: "ACME Supplies", supplierAddress: "Manila", supplierTin: null,
            modeOfProcurement: ModeOfProcurement.SmallValueProcurement, placeOfDelivery: "HQ",
            dateOfDelivery: null, deliveryTerm: "30 days", paymentTerm: "COD", fundCluster: null,
            oursBursNumber: null,
            lineItems: [new PurchaseOrderLineItemData(null, "unit", "Steel Cabinet", 1m, 5000m)]);

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        if (outcome == IarOutcome.None)
            return;

        var inspectorId = Guid.NewGuid();
        var iar = InspectionAcceptanceReport.Create(
            Tenant, $"IAR-{poNumber}", po.Id, supplierId: Guid.NewGuid(), supplierName: "ACME Supplies",
            inspectedById: inspectorId, receivedById: Guid.NewGuid(),
            deliveryReceiptNo: null, deliveryDate: null, remarks: null,
            lineItems:
            [
                new InspectionAcceptanceReportLineItemRequest(
                    Description: "Steel Cabinet", TechnicalSpecifications: null, Brand: null, Model: null,
                    SerialNo: null, PropertyClassHint: null, Unit: "unit", Quantity: 1m, UnitCost: 5000m,
                    InspectionRemarks: null)
            ],
            category: ProcurementCategory.Asset);

        // Drive the real workflow so Status lands exactly as it would in production. Accept() refuses to run until
        // every passed line carries a Stock/Property No, so assign one first.
        if (outcome == IarOutcome.Accepted)
        {
            iar.SubmitForInspection();
            iar.RecordInspection(inspectorId, [new LineInspectionDecision(1, LineInspectionResult.Passed, null)]);
            iar.AssignPropertyNo(1, $"PN-{poNumber}");
            iar.Accept(inspectorId, "Test Acceptor");
        }
        else
        {
            iar.Cancel();
        }

        db.InspectionAcceptanceReports.Add(iar);
        await db.SaveChangesAsync();
    }
}
