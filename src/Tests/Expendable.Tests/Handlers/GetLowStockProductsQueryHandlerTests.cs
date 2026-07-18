using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Warehouse;
using AMIS.Modules.Expendable.Features.v1.Warehouse.GetLowStockProducts;
using Expendable.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Expendable.Tests.Handlers;

/// <summary>
/// Verifies the reorder worklist: active products whose summed on-hand (available + reserved across all
/// warehouses) is at or below their configured minimum are returned, ordered by on-hand ascending. Runs
/// against real SQLite so the correlated-subquery translation is exercised, not just compiled.
/// </summary>
public sealed class GetLowStockProductsQueryHandlerTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_ReturnsActiveProductsAtOrBelowMinimum_OrderedByOnHand()
    {
        using var ctx = new ExpendableTestContext(Tenant);

        // A: minimum 10, on-hand 5  → below minimum, included
        var a = await SeedProductAsync(ctx.Db, "SKU-A", minimum: 10, reorder: 40);
        await SeedInventoryAsync(ctx.Db, a, onHand: 5);

        // B: minimum 10, on-hand 20 → above minimum, excluded
        var b = await SeedProductAsync(ctx.Db, "SKU-B", minimum: 10, reorder: 40);
        await SeedInventoryAsync(ctx.Db, b, onHand: 20);

        // C: minimum 5, never stocked (0 on-hand) → included, and sorts first (lowest on-hand)
        await SeedProductAsync(ctx.Db, "SKU-C", minimum: 5, reorder: 25);

        // D: no reorder point (minimum 0) → excluded regardless of stock
        await SeedProductAsync(ctx.Db, "SKU-D", minimum: 0, reorder: 0);

        var handler = new GetLowStockProductsQueryHandler(ctx.Db);
        var result = await handler.Handle(new GetLowStockProductsQuery(), CancellationToken.None);

        result.Select(r => r.StockNo).ShouldBe(["SKU-C", "SKU-A"]); // on-hand 0 then 5

        var c = result.Single(r => r.StockNo == "SKU-C");
        c.QuantityOnHand.ShouldBe(0);
        c.MinimumStockLevel.ShouldBe(5);
        c.ReorderQuantity.ShouldBe(25);
    }

    private static async Task<Guid> SeedProductAsync(ExpendableDbContext db, string stockNo, int minimum, int reorder)
    {
        // Raw SQL seed (xmin is the store-generated concurrency token — an EF insert omits it and trips
        // SQLite's NOT NULL; see SupplyIARAcceptedEventConsumerTests). Status 1 = Active.
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Products
  (Id, TenantId, StockNo, Article, Name, Description, UnitPrice, UnitOfMeasure,
   MinimumStockLevel, ReorderQuantity, Status, xmin, CreatedOnUtc, IsDeleted)
VALUES
  ({id}, {Tenant}, {stockNo}, 'Paper', {stockNo}, 'desc', 5, 'REAM',
   {minimum}, {reorder}, 1, 0, {DateTimeOffset.UtcNow}, 0)");
        return id;
    }

    private static async Task SeedInventoryAsync(ExpendableDbContext db, Guid productId, int onHand)
    {
        var inv = ProductInventory.Create(Tenant, productId, Guid.NewGuid());
        if (onHand > 0)
            inv.ReceiveFromPurchase(Guid.NewGuid(), onHand, 5m);
        db.ProductInventories.Add(inv);
        await db.SaveChangesAsync();
    }
}
