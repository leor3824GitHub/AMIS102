using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.SearchAcceptedIARLineItems;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Integration;

/// <summary>
/// Guards the AssetRegister boundary: the Receiving Report "From IAR" picker (fed by
/// <see cref="SearchAcceptedIARLineItemsQueryHandler"/>) must surface only <b>Asset</b> IAR lines now that
/// Supply IARs share the same table. The Supply line here deliberately carries a StockPropertyNo, so only
/// the explicit Category filter — not the incidental property-number filter — can exclude it.
/// </summary>
public sealed class SearchAcceptedIARLineItemsCategoryFilterTests
{
    private const string Tenant = "test-tenant";

    [Fact]
    public async Task Handle_ExcludesSupplyCategoryLines_EvenWhenTheyHaveAPropertyNo()
    {
        using var ctx = new ProcurementTestContext(Tenant);
        await SeedAcceptedIarAsync(ctx.Db, "IAR-ASSET", ProcurementCategory.Asset, stockPropertyNo: "PN-ASSET-1");
        await SeedAcceptedIarAsync(ctx.Db, "IAR-SUPPLY", ProcurementCategory.Supply, stockPropertyNo: "PN-SUPPLY-1");

        var handler = new SearchAcceptedIARLineItemsQueryHandler(ctx.Db);
        var result = await handler.Handle(new SearchAcceptedIARLineItemsQuery(), CancellationToken.None);

        var line = result.Items.ShouldHaveSingleItem();
        line.IARNumber.ShouldBe("IAR-ASSET");
        line.StockPropertyNo.ShouldBe("PN-ASSET-1");
    }

    private static async Task SeedAcceptedIarAsync(
        ProcurementDbContext db, string iarNumber, ProcurementCategory category, string stockPropertyNo)
    {
        var lineItem = new InspectionAcceptanceReportLineItemRequest(
            Description: "Steel Cabinet", TechnicalSpecifications: null, Brand: null, Model: null,
            SerialNo: null, PropertyClassHint: null, Unit: "unit", Quantity: 1m, UnitCost: 5000m,
            InspectionRemarks: null);

        var inspectorId = Guid.NewGuid();
        var iar = InspectionAcceptanceReport.Create(
            Tenant, iarNumber, purchaseOrderId: Guid.NewGuid(), supplierId: Guid.NewGuid(),
            supplierName: "ACME Supplies", inspectedById: inspectorId, receivedById: Guid.NewGuid(),
            deliveryReceiptNo: null, deliveryDate: null, remarks: null,
            lineItems: [lineItem], category: category);

        // Drive the real acceptance workflow so Status/AcceptedOnUtc/property numbers are set exactly as in
        // production. AssignPropertyNo is reachable for both categories at the entity level (the asset-only
        // gate lives at the command layer) — we use it here to put a StockPropertyNo on the Supply line too.
        iar.SubmitForInspection();
        iar.RecordInspection(inspectorId, [new LineInspectionDecision(1, LineInspectionResult.Passed, null)]);
        iar.AssignPropertyNo(1, stockPropertyNo);
        iar.Accept();

        db.InspectionAcceptanceReports.Add(iar);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// Stands up a real <see cref="ProcurementDbContext"/> over an isolated EF in-memory store with a fixed
/// tenant. The in-memory provider is used (not SQLite) because this query orders by <c>DateTimeOffset</c>,
/// which SQLite cannot translate, and because seeding through the domain workflow avoids the Postgres-only
/// <c>xmin</c> concurrency column.
/// </summary>
internal sealed class ProcurementTestContext : IDisposable
{
    public ProcurementDbContext Db { get; }

    public ProcurementTestContext(string tenantId)
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseInMemoryDatabase($"procurement-{Guid.NewGuid()}")
            .Options;

        var tenant = new AppTenantInfo(tenantId, tenantId, "Test Tenant");
        var accessor = new StaticTenantAccessor(new MultiTenantContext<AppTenantInfo>(tenant));
        var dbOptions = Options.Create(new DatabaseOptions { Provider = "InMemory" });

        Db = new ProcurementDbContext(accessor, options, dbOptions, new TestHostEnvironment());
    }

    public void Dispose() => Db.Dispose();

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
        public string ApplicationName { get; set; } = "ProcurementAcquisition.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
