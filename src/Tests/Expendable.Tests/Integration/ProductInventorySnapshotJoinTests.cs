using AMIS.Modules.Expendable;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Warehouse;
using AMIS.Modules.Expendable.Features.v1.Warehouse.GetWarehouseStockLevels;
using AMIS.Modules.Expendable.Features.v1.Warehouse.SearchProductInventory;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Expendable.Tests.Integration;

/// <summary>
/// Part A of the ledger restructure: <c>ProductInventory</c> no longer snapshots ProductCode/ProductName/
/// WarehouseLocationName — the Warehouse queries join <c>Product</c> and resolve the location name from the
/// constant. These tests prove (1) the join reflects the LIVE product name after a rename (the drift the
/// snapshot caused), (2) the code/name filters now run against the joined product, and (3) the join query
/// shapes translate on the SQLite harness. The DTO shape is unchanged, so no client/PDF change was needed.
/// </summary>
public sealed class ProductInventorySnapshotJoinTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Search_ReflectsLiveProductName_AfterRename_AndResolvesWarehouseName()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var productId = await SeedProductAsync(ctx.Db, "SN-1", "Bond Paper");
        await SeedInventoryAsync(ctx.Db, productId);

        var handler = new SearchProductInventoryQueryHandler(ctx.Db);

        var before = await handler.Handle(new SearchProductInventoryQuery(), CancellationToken.None);
        var row = before.Items.ShouldHaveSingleItem();
        row.ProductCode.ShouldBe("SN-1");
        row.ProductName.ShouldBe("Bond Paper");
        row.WarehouseLocationName.ShouldBe(ExpendableModuleConstants.DefaultSupplyLocation.Name);

        // Rename the product — the inventory listing must now show the new name (no stale snapshot copy).
        await ctx.Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Products SET Name = 'Copy Paper' WHERE Id = {productId}");

        var after = await handler.Handle(new SearchProductInventoryQuery(), CancellationToken.None);
        after.Items.ShouldHaveSingleItem().ProductName.ShouldBe("Copy Paper");
    }

    [Fact]
    public async Task Search_FiltersByLiveProductNameAndCode()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var bondId = await SeedProductAsync(ctx.Db, "SN-BOND", "Bond Paper");
        var inkId = await SeedProductAsync(ctx.Db, "SN-INK", "Ink Cartridge");
        await SeedInventoryAsync(ctx.Db, bondId);
        await SeedInventoryAsync(ctx.Db, inkId);

        var handler = new SearchProductInventoryQueryHandler(ctx.Db);

        var byName = await handler.Handle(new SearchProductInventoryQuery { ProductName = "Bond" }, CancellationToken.None);
        byName.Items.ShouldHaveSingleItem().ProductCode.ShouldBe("SN-BOND");

        var byCode = await handler.Handle(new SearchProductInventoryQuery { ProductCode = "SN-INK" }, CancellationToken.None);
        byCode.Items.ShouldHaveSingleItem().ProductName.ShouldBe("Ink Cartridge");
    }

    [Fact]
    public async Task GetWarehouseStockLevels_JoinsProduct_AndOrdersByStockNo()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var bId = await SeedProductAsync(ctx.Db, "SN-B", "Bravo");
        var aId = await SeedProductAsync(ctx.Db, "SN-A", "Alpha");
        await SeedInventoryAsync(ctx.Db, bId);
        await SeedInventoryAsync(ctx.Db, aId);

        var handler = new GetWarehouseStockLevelsQueryHandler(ctx.Db);
        var result = await handler.Handle(
            new GetWarehouseStockLevelsQuery { WarehouseLocationId = ExpendableModuleConstants.DefaultSupplyLocation.Id },
            CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items.ElementAt(0).ProductCode.ShouldBe("SN-A");   // ordered by joined StockNo
        result.Items.ElementAt(1).ProductCode.ShouldBe("SN-B");
    }

    private static async Task SeedInventoryAsync(ExpendableDbContext db, Guid productId)
    {
        var inv = ProductInventory.Create(Tenant, productId, ExpendableModuleConstants.DefaultSupplyLocation.Id);
        inv.ReceiveFromPurchase(Guid.NewGuid(), 10, 5m);
        db.ProductInventories.Add(inv);
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SeedProductAsync(ExpendableDbContext db, string stockNo, string name)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Products
  (Id, TenantId, StockNo, Article, Name, Description, UnitPrice, UnitOfMeasure,
   MinimumStockLevel, ReorderQuantity, Status, xmin, CreatedOnUtc, IsDeleted)
VALUES
  ({id}, {Tenant}, {stockNo}, 'Paper', {name}, 'desc', 5, 'REAM',
   1, 1, 1, 0, {DateTimeOffset.UtcNow}, 0)");
        return id;
    }
}
