using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementAcquisition.Domain.Canvass;
using FSH.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using FSH.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.ProcurementAcquisition.Data;

public class ProcurementDbContext : BaseDbContext
{
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<CanvassRequest> CanvassRequests => Set<CanvassRequest>();
    public DbSet<CanvassQuotation> CanvassQuotations => Set<CanvassQuotation>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

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
