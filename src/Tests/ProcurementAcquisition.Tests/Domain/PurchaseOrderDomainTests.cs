using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Domain;

public sealed class PurchaseOrderDomainTests
{
    [Fact]
    public void RecordDelivery_WhenAcceptedMeetsOrdered_MovesToFulfilled()
    {
        var po = CreateIssuedPo(quantity: 10m);

        po.RecordDelivery(10m);

        po.Status.ShouldBe(PurchaseOrderStatus.Fulfilled);
    }

    [Fact]
    public void RecordDelivery_WhenAcceptedExceedsOrdered_MovesToFulfilled()
    {
        var po = CreateIssuedPo(quantity: 10m);

        po.RecordDelivery(12m);

        po.Status.ShouldBe(PurchaseOrderStatus.Fulfilled);
    }

    [Fact]
    public void RecordDelivery_WhenAcceptedPartial_MovesToPartiallyDelivered()
    {
        var po = CreateIssuedPo(quantity: 10m);

        po.RecordDelivery(6m);

        po.Status.ShouldBe(PurchaseOrderStatus.PartiallyDelivered);
    }

    [Fact]
    public void RecordDelivery_CumulativeTotal_EventuallyFulfills()
    {
        var po = CreateIssuedPo(quantity: 10m);

        po.RecordDelivery(6m);  // partial delivery
        po.Status.ShouldBe(PurchaseOrderStatus.PartiallyDelivered);

        po.RecordDelivery(10m); // running total now meets the ordered quantity

        po.Status.ShouldBe(PurchaseOrderStatus.Fulfilled);
    }

    [Fact]
    public void RecordDelivery_WhenZeroAccepted_StaysIssued()
    {
        var po = CreateIssuedPo(quantity: 10m);

        po.RecordDelivery(0m);

        po.Status.ShouldBe(PurchaseOrderStatus.Issued);
    }

    [Fact]
    public void RecordDelivery_WhenDraft_NoOps()
    {
        var po = CreateDraftPo(quantity: 10m);

        po.RecordDelivery(100m);

        po.Status.ShouldBe(PurchaseOrderStatus.Draft);
    }

    [Fact]
    public void RecordDelivery_WhenAlreadyFulfilled_DoesNotRegress()
    {
        var po = CreateIssuedPo(quantity: 10m);
        po.RecordDelivery(10m); // Fulfilled

        po.RecordDelivery(3m);  // a late, smaller figure must not downgrade it

        po.Status.ShouldBe(PurchaseOrderStatus.Fulfilled);
    }

    [Fact]
    public void Issue_FreezesAuthorizedOfficialDesignation()
    {
        var po = CreateDraftPo();
        po.Submit();
        po.CertifyFundsAvailable(Guid.NewGuid(), "Jane Accountant", null, null, null,
            certifiedByDesignation: "Accountant IV");
        po.Issue(Guid.NewGuid(), "John Officer", "Regional Director");

        po.IssuedByName.ShouldBe("John Officer");
        po.IssuedByDesignation.ShouldBe("Regional Director");                       // frozen at issue
        po.FundsAvailableCertifiedByDesignation.ShouldBe("Accountant IV");          // frozen at certify
    }

    private static PurchaseOrder CreateDraftPo(decimal quantity = 10m) =>
        PurchaseOrder.Create(
            tenantId: "tenant-1",
            poNumber: "PO-2026-001",
            purchaseRequestId: Guid.NewGuid(),
            canvassRequestId: null,
            supplierId: Guid.NewGuid(),
            supplierName: "Acme Supplies",
            supplierAddress: "123 Main St",
            supplierTin: null,
            modeOfProcurement: ModeOfProcurement.SmallValueProcurement,
            placeOfDelivery: "Central Warehouse",
            dateOfDelivery: null,
            deliveryTerm: "30 days",
            paymentTerm: "Net 30",
            fundCluster: null,
            oursBursNumber: null,
            lineItems: [new PurchaseOrderLineItemData(null, "piece", "Bond Paper A4", quantity, 100m)]);

    private static PurchaseOrder CreateIssuedPo(decimal quantity = 10m)
    {
        var po = CreateDraftPo(quantity);
        po.Submit();
        po.CertifyFundsAvailable(Guid.NewGuid(), "Jane Accountant", null, null, null);
        po.Issue(Guid.NewGuid(), "John Officer");
        return po;
    }
}
