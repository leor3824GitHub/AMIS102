using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Domain.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.ProcurementAcquisition.Data;

public class ProcurementDbContext : BaseDbContext
{
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<CanvassRequest> CanvassRequests => Set<CanvassRequest>();
    public DbSet<CanvassQuotation> CanvassQuotations => Set<CanvassQuotation>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<AssetInspectionAcceptanceReport> AssetIARs => Set<AssetInspectionAcceptanceReport>();

    public ProcurementDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ProcurementDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcurementDbContext).Assembly);
    }
}

