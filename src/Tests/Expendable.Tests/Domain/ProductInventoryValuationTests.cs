using AMIS.Modules.Expendable.Domain.Warehouse;
using Shouldly;
using Xunit;

namespace Expendable.Tests.Domain;

/// <summary>
/// Locks the <see cref="ProductInventory"/> money path — moving-average valuation, the reserve/issue
/// lifecycle, the derived <c>AverageUnitPrice</c>/<c>ReservedValue</c> getters, and the batch ledger.
/// This is the safety net for the deferred inventory-ledger restructure (Phase 4 batch archival +
/// structural Phase 5): these assertions must still hold after batches are pruned/archived.
/// </summary>
public sealed class ProductInventoryValuationTests
{
    private const string Tenant = "test-tenant";

    private static ProductInventory NewInventory() => ProductInventory.Create(
        Tenant, Guid.NewGuid(), Guid.NewGuid());

    // ── Receipts & moving-average valuation ──────────────────────────────────────────────────

    [Fact]
    public void Create_StartsEmpty_AtZeroValue()
    {
        var inv = NewInventory();

        inv.QuantityAvailable.ShouldBe(0);
        inv.QuantityReserved.ShouldBe(0);
        inv.QuantityIssued.ShouldBe(0);
        inv.QuantityOnHand.ShouldBe(0);
        inv.TotalValue.ShouldBe(0m);
        inv.AverageUnitPrice.ShouldBe(0m);
        inv.ReservedValue.ShouldBe(0m);
        inv.Status.ShouldBe(ProductInventoryStatus.Active);
        inv.Batches.ShouldBeEmpty();
    }

    [Fact]
    public void ReceiveFromPurchase_AddsBatch_AndIncrementsAvailableAndValue()
    {
        var inv = NewInventory();

        inv.ReceiveFromPurchase(Guid.NewGuid(), quantityAccepted: 10, unitPrice: 5m, sourceReference: "IAR-0001");

        inv.QuantityAvailable.ShouldBe(10);
        inv.QuantityOnHand.ShouldBe(10);
        inv.TotalValue.ShouldBe(50m);
        inv.AverageUnitPrice.ShouldBe(5m);
        inv.Batches.Count.ShouldBe(1);
        inv.Batches.Single().QuantityAvailable.ShouldBe(10);
        inv.Batches.Single().UnitPrice.ShouldBe(5m);
        inv.Batches.Single().SourceReference.ShouldBe("IAR-0001");
        inv.FirstReceiptDate.ShouldNotBeNull();
        inv.LastReceiptDate.ShouldNotBeNull();
    }

    [Fact]
    public void ReceiveFromPurchase_TwoReceiptsAtDifferentPrices_ComputesMovingAverage()
    {
        var inv = NewInventory();

        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 7m);

        inv.QuantityAvailable.ShouldBe(20);
        inv.TotalValue.ShouldBe(120m);
        inv.AverageUnitPrice.ShouldBe(6m);   // (50 + 70) / 20
        inv.Batches.Count.ShouldBe(2);
    }

    [Fact]
    public void FirstReceiptDate_IsPinnedToFirstReceipt_LastReceiptDateTracksLatest()
    {
        var inv = NewInventory();

        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        var firstReceipt = inv.FirstReceiptDate;

        inv.ReceiveFromPurchase(Guid.NewGuid(), 5, 7m);

        inv.FirstReceiptDate.ShouldBe(firstReceipt);              // pinned
        inv.LastReceiptDate!.Value.ShouldBeGreaterThanOrEqualTo(firstReceipt!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReceiveFromPurchase_NonPositiveQuantity_Throws(int qty)
    {
        var inv = NewInventory();
        Should.Throw<ArgumentException>(() => inv.ReceiveFromPurchase(Guid.NewGuid(), qty, 5m));
    }

    [Fact]
    public void ReceiveFromPurchase_NegativeUnitPrice_Throws()
    {
        var inv = NewInventory();
        Should.Throw<ArgumentException>(() => inv.ReceiveFromPurchase(Guid.NewGuid(), 10, -1m));
    }

    // ── Reservation lifecycle ────────────────────────────────────────────────────────────────

    [Fact]
    public void ReserveForAllocation_MovesAvailableToReserved_AndReservedValueTracksAverage()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 7m);   // avg 6

        inv.ReserveForAllocation(5);

        inv.QuantityAvailable.ShouldBe(15);
        inv.QuantityReserved.ShouldBe(5);
        inv.QuantityOnHand.ShouldBe(20);            // reserved stock is still on hand
        inv.AverageUnitPrice.ShouldBe(6m);
        inv.ReservedValue.ShouldBe(30m);            // 5 × 6.00
        inv.AvailableForAllocation.ShouldBe(15);
    }

    [Fact]
    public void CancelReservation_ReturnsReservedToAvailable()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReserveForAllocation(6);

        inv.CancelReservation(4);

        inv.QuantityAvailable.ShouldBe(8);
        inv.QuantityReserved.ShouldBe(2);
    }

    [Fact]
    public void ReserveForAllocation_MoreThanAvailable_Throws()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);

        Should.Throw<InvalidOperationException>(() => inv.ReserveForAllocation(11));
    }

    [Fact]
    public void CancelReservation_MoreThanReserved_Throws()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReserveForAllocation(3);

        Should.Throw<InvalidOperationException>(() => inv.CancelReservation(4));
    }

    // ── Issuance (moving-average draw-down) ──────────────────────────────────────────────────

    [Fact]
    public void IssueReservedStock_DrawsDownAtAverageCost_AndReturnsIssuanceDetail()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 7m);   // total 120, avg 6
        inv.ReserveForAllocation(5);

        var detail = inv.IssueReservedStock(5);

        detail.QuantityIssued.ShouldBe(5);
        detail.UnitPrice.ShouldBe(6m);
        detail.TotalValue.ShouldBe(30m);

        inv.QuantityReserved.ShouldBe(0);
        inv.QuantityIssued.ShouldBe(5);
        inv.QuantityAvailable.ShouldBe(15);
        inv.QuantityOnHand.ShouldBe(15);
        inv.TotalValue.ShouldBe(90m);               // 120 − 30
        inv.AverageUnitPrice.ShouldBe(6m);          // unchanged by an at-average draw
        inv.LastIssueDate.ShouldNotBeNull();
    }

    [Fact]
    public void IssueReservedStock_MoreThanReserved_Throws()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReserveForAllocation(3);

        Should.Throw<InvalidOperationException>(() => inv.IssueReservedStock(4));
    }

    [Fact]
    public void TotalValue_NeverGoesNegative_OnRoundingDrawDown()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 3, 0.01m);   // total 0.03
        inv.ReserveForAllocation(3);

        inv.IssueReservedStock(3);

        inv.TotalValue.ShouldBeGreaterThanOrEqualTo(0m);
        inv.QuantityOnHand.ShouldBe(0);
        inv.AverageUnitPrice.ShouldBe(0m);          // no stock on hand ⇒ zero, not a divide-by-zero
    }

    // ── Discontinue guard ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Discontinue_WithStockRemaining_Throws()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);

        Should.Throw<InvalidOperationException>(() => inv.Discontinue());
    }

    [Fact]
    public void Discontinue_WhenEmpty_SetsDiscontinued()
    {
        var inv = NewInventory();
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        inv.ReserveForAllocation(10);
        inv.IssueReservedStock(10);

        inv.Discontinue();

        inv.Status.ShouldBe(ProductInventoryStatus.Discontinued);
    }
}
