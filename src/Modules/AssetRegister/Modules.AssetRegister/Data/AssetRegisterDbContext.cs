using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Domain.Accountability;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using AMIS.Modules.AssetRegister.Domain.Counting;
using AMIS.Modules.AssetRegister.Domain.Incidents;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using AMIS.Modules.AssetRegister.Domain.Locations;
using AMIS.Modules.AssetRegister.Domain.Receiving;
using AMIS.Modules.AssetRegister.Domain.ReturnedProperty;
using AMIS.Modules.AssetRegister.Domain.Unserviceable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.AssetRegister.Data;

public class AssetRegisterDbContext : BaseDbContext
{
    public DbSet<PropertyItemCatalog> PropertyItemCatalogs => Set<PropertyItemCatalog>();
    public DbSet<PropertyCodeCounter> PropertyCodeCounters => Set<PropertyCodeCounter>();
    public DbSet<AssetRegistry> AssetRegistries => Set<AssetRegistry>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();
    public DbSet<PropertyAccountability> PropertyAccountabilities => Set<PropertyAccountability>();
    public DbSet<PropertyIssuanceReport> PropertyIssuanceReports => Set<PropertyIssuanceReport>();
    public DbSet<PPEIRFormSeries> PPEIRFormSeries => Set<PPEIRFormSeries>();
    public DbSet<PhysicalCountSession> PhysicalCountSessions => Set<PhysicalCountSession>();
    public DbSet<PropertyIncidentReport> PropertyIncidentReports => Set<PropertyIncidentReport>();
    public DbSet<UnserviceablePropertyReport> UnserviceablePropertyReports => Set<UnserviceablePropertyReport>();
    public DbSet<Domain.Repairs.PropertyRepair> PropertyRepairs => Set<Domain.Repairs.PropertyRepair>();
    public DbSet<ReceivingReport> ReceivingReports => Set<ReceivingReport>();
    public DbSet<PPERRFormSeries> PPERRFormSeries => Set<PPERRFormSeries>();
    public DbSet<ReturnedPropertyReceipt> ReturnedPropertyReceipts => Set<ReturnedPropertyReceipt>();
    public DbSet<Location> Locations => Set<Location>();

    public AssetRegisterDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<AssetRegisterDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetRegisterDbContext).Assembly);

        // Postgres-only: pg_trgm GIN indexes accelerate the leading-wildcard ILIKE keyword search that
        // SearchAssets runs over these columns (a b-tree can't serve '%kw%'). Guarded by provider so the
        // EF InMemory test provider skips the unsupported 'USING gin' DDL.
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            modelBuilder.HasPostgresExtension("pg_trgm");
            modelBuilder.Entity<AssetRegistry>().HasIndex(a => a.Description)
                .HasDatabaseName("IX_AssetRegistries_Description_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
            modelBuilder.Entity<AssetRegistry>().HasIndex(a => a.SerialNo)
                .HasDatabaseName("IX_AssetRegistries_SerialNo_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
        }
    }
}

