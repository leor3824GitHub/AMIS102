using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Warehouse;
using AMIS.Modules.Expendable.Features.v1.Reports.GetStockCard;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Expendable.Tests.Integration;

/// <summary>
/// Locks the receipt/batch half of the Stock Card ledger: every <see cref="ProductInventory"/> batch surfaces
/// as a "Receipt" line with the moving-average running balance. This is the exact behaviour the deferred
/// Phase 4 batch archival must preserve ("the Stock Card reads archive ∪ open batches") — the assertions here
/// pin what "∪" has to reproduce once exhausted batches move to a relational archive table.
///
/// The issuance half (fulfilled <c>SupplyRequest</c> items) is intentionally out of scope: archival does not
/// restructure that path, and seeding the full Approve→Fulfill lifecycle would over-fit this net.
/// </summary>
public sealed class ProductInventoryStockCardTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task StockCard_SurfacesEveryBatchAsReceipt_WithMovingAverageBalance()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var productId = await SeedProductAsync(ctx.Db, "SN-SC", "Bond Paper", "REAM");

        var inventory = ProductInventory.Create(Tenant, productId, Guid.NewGuid());
        inventory.ReceiveFromPurchase(Guid.NewGuid(), productId, 10, 5m, "IAR-SC1");
        inventory.ReceiveFromPurchase(Guid.NewGuid(), productId, 10, 7m, "IAR-SC2");
        ctx.Db.ProductInventories.Add(inventory);
        await ctx.Db.SaveChangesAsync();

        var handler = new GetStockCardQueryHandler(ctx.Db);
        var card = await handler.Handle(new GetStockCardQuery(productId), CancellationToken.None);

        card.ShouldNotBeNull();
        card!.ProductId.ShouldBe(productId);
        card.ProductCode.ShouldBe("SN-SC");
        card.ProductName.ShouldBe("Bond Paper");
        card.UnitOfMeasure.ShouldBe("REAM");

        card.Lines.Count.ShouldBe(2);
        card.Lines.ShouldAllBe(l => l.TransactionType == "Receipt");
        card.Lines.Select(l => l.Reference).ShouldBe(["IAR-SC1", "IAR-SC2"], ignoreOrder: true);
        card.Lines.Sum(l => l.ReceiptQty).ShouldBe(20);
        card.Lines.Sum(l => l.ReceiptTotalCost).ShouldBe(120m);

        // Final running balance is order-independent: 20 on hand, 120.00 value, 6.00 moving-average unit cost.
        var last = card.Lines[^1];
        last.BalanceQty.ShouldBe(20);
        last.BalanceTotalCost.ShouldBe(120m);
        last.BalanceUnitCost.ShouldBe(6m);
    }

    [Fact]
    public async Task StockCard_UnknownProduct_ReturnsNull()
    {
        using var ctx = new ExpendableTestContext(Tenant);

        var handler = new GetStockCardQueryHandler(ctx.Db);
        var card = await handler.Handle(new GetStockCardQuery(Guid.NewGuid()), CancellationToken.None);

        card.ShouldBeNull();
    }

    /// <summary>
    /// Seeds a Product via raw SQL with an explicit <c>xmin</c>. On SQLite <c>EnsureCreated</c> the Postgres
    /// system-column concurrency token materializes as a real NOT NULL column that an EF insert would omit;
    /// production runs on Postgres where <c>xmin</c> is auto-maintained. Mirrors the seed in
    /// <see cref="SupplyIARAcceptedEventConsumerTests"/>.
    /// </summary>
    private static async Task<Guid> SeedProductAsync(ExpendableDbContext db, string stockNo, string name, string uom)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Products
  (Id, TenantId, StockNo, Article, Name, Description, UnitPrice, UnitOfMeasure,
   MinimumStockLevel, ReorderQuantity, Status, xmin, CreatedOnUtc, IsDeleted)
VALUES
  ({id}, {Tenant}, {stockNo}, 'Paper', {name}, 'desc', 5, {uom},
   1, 1, 1, 0, {DateTimeOffset.UtcNow}, 0)");

        return id;
    }
}
