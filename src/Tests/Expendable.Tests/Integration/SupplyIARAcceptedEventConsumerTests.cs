using AMIS.Framework.Caching;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using AMIS.Modules.Expendable.Integration;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Expendable.Tests.Integration;

/// <summary>
/// Verifies the ProcurementAcquisition → Expendable seam: accepted Supply IAR lines land into
/// <c>ProductInventory</c>, and a redelivered event (same IARId) does not double-count — the heart
/// of the idempotency guard added in <see cref="SupplyIARAcceptedEventConsumer"/>.
/// </summary>
public sealed class SupplyIARAcceptedEventConsumerTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task HandleAsync_AcceptedSupplyLine_LandsStockIntoProductInventory()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var product = await SeedProductAsync(ctx.Db, "SN-1");

        var consumer = NewConsumer(ctx.Db);
        await consumer.HandleAsync(NewEvent(Guid.NewGuid(), "SN-1", quantity: 10, unitCost: 5m));

        var inventory = await ctx.Db.ProductInventories.SingleAsync(pi => pi.ProductId == product.Id);
        inventory.QuantityAvailable.ShouldBe(10);
        inventory.Batches.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_RedeliveredEventWithSameIARId_DoesNotDoubleCount()
    {
        using var ctx = new ExpendableTestContext(Tenant);
        var product = await SeedProductAsync(ctx.Db, "SN-1");

        var consumer = NewConsumer(ctx.Db);
        var iarId = Guid.NewGuid();

        await consumer.HandleAsync(NewEvent(iarId, "SN-1", quantity: 10, unitCost: 5m));
        // Redelivery: a *fresh* event instance (so @event.Id differs) carrying the SAME IARId. This proves
        // the guard keys on the stable business id, not the volatile event id that the framework inbox uses.
        await consumer.HandleAsync(NewEvent(iarId, "SN-1", quantity: 10, unitCost: 5m));

        var inventory = await ctx.Db.ProductInventories.SingleAsync(pi => pi.ProductId == product.Id);
        inventory.QuantityAvailable.ShouldBe(10);
        inventory.Batches.Count.ShouldBe(1);
        (await ctx.Db.InboxMessages.CountAsync()).ShouldBe(1);
    }

    private static async Task<Product> SeedProductAsync(ExpendableDbContext db, string stockNo)
    {
        // Product's concurrency token is the Postgres system column xmin (store-generated,
        // ValueGeneratedOnAddOrUpdate). On Postgres it needs no DDL/value; on SQLite EnsureCreated
        // materializes a real NOT NULL "xmin" column that an EF insert omits (it expects the store to
        // generate it) → NOT NULL violation. Seed via raw SQL with an explicit xmin instead — this is a
        // SQLite-test-only concern; production runs on Postgres where xmin is auto-maintained.
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Products
  (Id, TenantId, StockNo, Article, Name, Description, UnitPrice, UnitOfMeasure,
   MinimumStockLevel, ReorderQuantity, Status, xmin, CreatedOnUtc, IsDeleted)
VALUES
  ({id}, {Tenant}, {stockNo}, 'Paper', 'Bond Paper', 'desc', 5, 'REAM',
   1, 1, 1, 0, {DateTimeOffset.UtcNow}, 0)");

        return await db.Products.SingleAsync(p => p.StockNo == stockNo);
    }

    private static SupplyIARAcceptedEventConsumer NewConsumer(ExpendableDbContext db) =>
        new(db, Substitute.For<ICacheService>(), NullLogger<SupplyIARAcceptedEventConsumer>.Instance);

    private static SupplyIARAcceptedEvent NewEvent(Guid iarId, string stockNo, decimal quantity, decimal unitCost) =>
        new(
            IARId: iarId,
            IarNumber: "IAR-0001",
            PurchaseOrderId: Guid.NewGuid(),
            PoNumber: "PO-0001",
            SupplierId: Guid.NewGuid(),
            SupplierName: "ACME Supplies",
            AcceptedItems: [new SupplyIARAcceptedEventItem(stockNo, "Bond Paper", "REAM", quantity, unitCost)],
            TenantId: Tenant);
}

/// <summary>
/// Stands up a real <see cref="ExpendableDbContext"/> over an isolated in-memory SQLite database with a
/// fixed tenant. SQLite (not EF InMemory) is used because the model relies on relational JSON columns
/// (<c>ToJson</c> for inventory batches) and a composite primary key on InboxMessages.
/// </summary>
internal sealed class ExpendableTestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public ExpendableDbContext Db { get; }

    public ExpendableTestContext(string tenantId)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ExpendableDbContext>()
            .UseSqlite(_connection)
            .Options;

        var tenant = new AppTenantInfo(tenantId, tenantId, "Test Tenant");
        var accessor = new StaticTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions { Provider = "Sqlite" });

        Db = new ExpendableDbContext(accessor, options, dbOptions, new TestHostEnvironment());
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    private sealed class StaticTenantAccessor(IMultiTenantContext<AppTenantInfo> context)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = context;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Expendable.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
