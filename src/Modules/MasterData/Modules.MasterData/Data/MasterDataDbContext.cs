using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.MasterData.Data;

public class MasterDataDbContext : BaseDbContext
{
    public DbSet<EmployeeProfile> Employees => Set<EmployeeProfile>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ReportSignatory> ReportSignatories => Set<ReportSignatory>();

    public MasterDataDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<MasterDataDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDataDbContext).Assembly);
    }
}


