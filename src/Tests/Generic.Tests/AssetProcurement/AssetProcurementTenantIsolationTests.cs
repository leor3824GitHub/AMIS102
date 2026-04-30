using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Generic.Tests.AssetProcurement;

public sealed class AssetProcurementTenantIsolationTests
{
    [Fact]
    public async Task AssetPurchaseRequests_WithSameNumberAcrossTenants_ReturnOnlyCurrentTenantLineItems()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString("N");
        await SeedPurchaseRequestAsync(databaseName, "tenant-a", "A4 Bond Paper");
        await SeedPurchaseRequestAsync(databaseName, "tenant-b", "Ballpen Blue");

        // Act
        await using var tenantAContext = CreateDbContext(databaseName, "tenant-a");
        var tenantARequests = await tenantAContext.AssetPurchaseRequests.AsNoTracking().ToListAsync();

        await using var tenantBContext = CreateDbContext(databaseName, "tenant-b");
        var tenantBRequests = await tenantBContext.AssetPurchaseRequests.AsNoTracking().ToListAsync();

        // Assert
        tenantARequests.Count.ShouldBe(1);
        tenantARequests[0].LineItems.Count.ShouldBe(1);
        tenantARequests[0].LineItems[0].ItemDescription.ShouldBe("A4 Bond Paper");

        tenantBRequests.Count.ShouldBe(1);
        tenantBRequests[0].LineItems.Count.ShouldBe(1);
        tenantBRequests[0].LineItems[0].ItemDescription.ShouldBe("Ballpen Blue");
    }

    [Fact]
    public async Task AssetPurchaseOrders_WithSameNumberAcrossTenants_ReturnOnlyCurrentTenantLineItems()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString("N");
        await SeedPurchaseOrderAsync(databaseName, "tenant-a", "Laptop i5");
        await SeedPurchaseOrderAsync(databaseName, "tenant-b", "Desktop i7");

        // Act
        await using var tenantAContext = CreateDbContext(databaseName, "tenant-a");
        var tenantAOrders = await tenantAContext.AssetPurchaseOrders.AsNoTracking().ToListAsync();

        await using var tenantBContext = CreateDbContext(databaseName, "tenant-b");
        var tenantBOrders = await tenantBContext.AssetPurchaseOrders.AsNoTracking().ToListAsync();

        // Assert
        tenantAOrders.Count.ShouldBe(1);
        tenantAOrders[0].LineItems.Count.ShouldBe(1);
        tenantAOrders[0].LineItems[0].Description.ShouldBe("Laptop i5");

        tenantBOrders.Count.ShouldBe(1);
        tenantBOrders[0].LineItems.Count.ShouldBe(1);
        tenantBOrders[0].LineItems[0].Description.ShouldBe("Desktop i7");
    }

    [Fact]
    public async Task AssetIARs_WithSameNumberAcrossTenants_ReturnOnlyCurrentTenantLineItems()
    {
        // Arrange
        var databaseName = Guid.NewGuid().ToString("N");
        await SeedIarAsync(databaseName, "tenant-a", "Printer LaserJet");
        await SeedIarAsync(databaseName, "tenant-b", "Scanner Flatbed");

        // Act
        await using var tenantAContext = CreateDbContext(databaseName, "tenant-a");
        var tenantAIars = await tenantAContext.AssetIARs.AsNoTracking().ToListAsync();

        await using var tenantBContext = CreateDbContext(databaseName, "tenant-b");
        var tenantBIars = await tenantBContext.AssetIARs.AsNoTracking().ToListAsync();

        // Assert
        tenantAIars.Count.ShouldBe(1);
        tenantAIars[0].LineItems.Count.ShouldBe(1);
        tenantAIars[0].LineItems[0].Description.ShouldBe("Printer LaserJet");

        tenantBIars.Count.ShouldBe(1);
        tenantBIars[0].LineItems.Count.ShouldBe(1);
        tenantBIars[0].LineItems[0].Description.ShouldBe("Scanner Flatbed");
    }

    private static async Task SeedPurchaseRequestAsync(string databaseName, string tenantId, string itemDescription)
    {
        await using var context = CreateDbContext(databaseName, tenantId);

        var request = AssetPurchaseRequest.Create(
            tenantId,
            "APR-2026-0001",
            Guid.NewGuid(),
            "ICT",
            "Office supply request",
            AssetPrType.Planned,
            null,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            [new AssetPurchaseRequestLineItemRequest(itemDescription, null, null, null, null, "box", 2, 150)]);

        context.AssetPurchaseRequests.Add(request);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPurchaseOrderAsync(string databaseName, string tenantId, string lineItemDescription)
    {
        await using var context = CreateDbContext(databaseName, tenantId);

        var order = AssetPurchaseOrder.Create(
            tenantId,
            "APO-2026-0001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ABC Supplier",
            "Main Road",
            null,
            AssetModeOfProcurement.ShoppingA,
            "City Hall",
            null,
            "15 days",
            "30 days",
            null,
            null,
            [new AssetPurchaseOrderLineItemRequest("unit", lineItemDescription, null, null, null, null, 1, 50000)]);

        context.AssetPurchaseOrders.Add(order);
        await context.SaveChangesAsync();
    }

    private static async Task SeedIarAsync(string databaseName, string tenantId, string lineItemDescription)
    {
        await using var context = CreateDbContext(databaseName, tenantId);

        var iar = AssetInspectionAcceptanceReport.Create(
            tenantId,
            "IAR-2026-0001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ABC Supplier",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            [new AssetIARLineItemRequest(lineItemDescription, null, null, null, null, null, "unit", 1, 25000, null)]);

        context.AssetIARs.Add(iar);
        await context.SaveChangesAsync();
    }

    private static AssetProcurementDbContext CreateDbContext(string databaseName, string tenantId)
    {
        var options = new DbContextOptionsBuilder<AssetProcurementDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var tenant = new AppTenantInfo(tenantId, tenantId, tenantId)
        {
            IsActive = true,
            ValidUpto = DateTime.UtcNow.AddYears(1)
        };

        var tenantAccessor = new TestTenantContextAccessor(new MultiTenantContext<AppTenantInfo>(tenant));

        var databaseOptions = Options.Create(new DatabaseOptions
        {
            Provider = DbProviders.PostgreSQL,
            MigrationsAssembly = "Generic.Tests"
        });

        return new AssetProcurementDbContext(tenantAccessor, options, databaseOptions, new TestHostEnvironment());
    }

    private sealed class TestTenantContextAccessor(IMultiTenantContext<AppTenantInfo> multiTenantContext)
        : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext MultiTenantContext { get; } = multiTenantContext;

        IMultiTenantContext<AppTenantInfo> IMultiTenantContextAccessor<AppTenantInfo>.MultiTenantContext =>
            (IMultiTenantContext<AppTenantInfo>)MultiTenantContext;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = nameof(AssetProcurementTenantIsolationTests);
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
