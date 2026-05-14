using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using Shouldly;
using Xunit;

namespace AssetProcurement.Tests.Domain;

public sealed class AssetIARAcceptStockPropertyNoTests
{
    [Fact]
    public void Accept_WithAllLinesNumbered_Succeeds()
    {
        var iar = NewDraft(
            new AssetIARLineItemRequest("Desk", null, null, null, null, null, "pc", 1, 5000m, null, "2026-NFA-00B-07-DSK-001"),
            new AssetIARLineItemRequest("Chair", null, null, null, null, null, "pc", 1, 1500m, null, "2026-NFA-00B-07-CHR-001"));

        Should.NotThrow(() => iar.Accept());
        iar.Status.ShouldBe(AssetIARStatus.Accepted);
    }

    [Fact]
    public void Accept_WithMissingStockPropertyNo_Throws()
    {
        var iar = NewDraft(
            new AssetIARLineItemRequest("Desk", null, null, null, null, null, "pc", 1, 5000m, null, "2026-NFA-00B-07-DSK-001"),
            new AssetIARLineItemRequest("Chair", null, null, null, null, null, "pc", 1, 1500m, null, null));

        var ex = Should.Throw<InvalidOperationException>(() => iar.Accept());
        ex.Message.ShouldContain("Stock/Property No is required");
        ex.Message.ShouldContain("2"); // ItemNo of the offending line
    }

    [Fact]
    public void Accept_WithWhitespaceStockPropertyNo_Throws()
    {
        var iar = NewDraft(
            new AssetIARLineItemRequest("Desk", null, null, null, null, null, "pc", 1, 5000m, null, "   "));

        Should.Throw<InvalidOperationException>(() => iar.Accept());
    }

    [Fact]
    public void Create_TrimsStockPropertyNo()
    {
        var iar = NewDraft(
            new AssetIARLineItemRequest("Desk", null, null, null, null, null, "pc", 1, 5000m, null, "  2026-NFA-00B-07-DSK-001  "));

        iar.LineItems[0].StockPropertyNo.ShouldBe("2026-NFA-00B-07-DSK-001");
    }

    private static AssetInspectionAcceptanceReport NewDraft(params AssetIARLineItemRequest[] lines) =>
        AssetInspectionAcceptanceReport.Create(
            tenantId: "root",
            iarNumber: "IAR-2026-0001",
            purchaseOrderId: Guid.NewGuid(),
            supplierId: Guid.NewGuid(),
            supplierName: "ACME Office Supplies",
            inspectedById: Guid.NewGuid(),
            receivedById: Guid.NewGuid(),
            deliveryReceiptNo: "DR-001",
            deliveryDate: new DateOnly(2026, 5, 14),
            remarks: null,
            lineItems: lines);
}
