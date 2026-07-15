using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.MasterData.Data;

public class MasterDataDbContext : BaseDbContext
{
    public DbSet<EmployeeProfile> Employees => Set<EmployeeProfile>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ModeOfProcurement> ModesOfProcurement => Set<ModeOfProcurement>();
    public DbSet<ReportSignatory> ReportSignatories => Set<ReportSignatory>();
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();
    public DbSet<CapitalizationThreshold> CapitalizationThresholds => Set<CapitalizationThreshold>();
    public DbSet<PropertyClass> PropertyClasses => Set<PropertyClass>();
    public DbSet<PropertyClassItem> PropertyClassItems => Set<PropertyClassItem>();
    public DbSet<FundCluster> FundClusters => Set<FundCluster>();
    public DbSet<FundingSourceCode> FundingSourceCodes => Set<FundingSourceCode>();

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

        // SQLite test harness only: 'xmin' (EmployeeProfile's concurrency token) is a Postgres system
        // column auto-maintained by the store. Under SQLite's EnsureCreated it materializes as a real
        // NOT NULL column with no store generation, so an EF insert that omits it trips a NOT NULL
        // violation. Demote it to a plain column on SQLite so EF sends the shadow default (0). No effect
        // on Postgres. (Mirrors the identical shim in ExpendableDbContext.)
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var xmin = entityType.FindProperty("xmin");
                if (xmin is not null)
                    xmin.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}



