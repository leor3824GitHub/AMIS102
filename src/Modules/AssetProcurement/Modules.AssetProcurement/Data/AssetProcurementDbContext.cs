using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.AssetProcurement.Data;

public class AssetProcurementDbContext : BaseDbContext
{
    public DbSet<AssetPurchaseRequest> AssetPurchaseRequests => Set<AssetPurchaseRequest>();
    public DbSet<AssetPurchaseOrder> AssetPurchaseOrders => Set<AssetPurchaseOrder>();
    public DbSet<AssetInspectionAcceptanceReport> AssetIARs => Set<AssetInspectionAcceptanceReport>();

    public AssetProcurementDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<AssetProcurementDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : base(multiTenantContextAccessor, options, settings, environment)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetProcurementDbContext).Assembly);
    }
}
