using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.ProcurementPlanning.Data;

public class ProcurementPlanningDbContext : BaseDbContext
{
    public DbSet<Ppmp> Ppmps => Set<Ppmp>();
    public DbSet<PpmpItem> PpmpItems => Set<PpmpItem>();
    public DbSet<AnnualProcurementPlan> AnnualProcurementPlans => Set<AnnualProcurementPlan>();
    public DbSet<AppSourcePpmp> AppSourcePpmps => Set<AppSourcePpmp>();
    public DbSet<AppLineItem> AppLineItems => Set<AppLineItem>();

    public ProcurementPlanningDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ProcurementPlanningDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcurementPlanningDbContext).Assembly);
    }
}

