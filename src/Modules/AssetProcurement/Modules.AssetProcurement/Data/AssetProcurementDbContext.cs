using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Domain.AssetPurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.AssetProcurement.Data;

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

