using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Domain.Accountability;
using FSH.Modules.AssetRegister.Domain.Assets;
using FSH.Modules.AssetRegister.Domain.Catalog;
using FSH.Modules.AssetRegister.Domain.Counting;
using FSH.Modules.AssetRegister.Domain.Incidents;
using FSH.Modules.AssetRegister.Domain.Issuance;
using FSH.Modules.AssetRegister.Domain.Receiving;
using FSH.Modules.AssetRegister.Domain.Unserviceable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.AssetRegister.Data;

public class AssetRegisterDbContext : BaseDbContext
{
    public DbSet<PropertyItemCatalog> PropertyItemCatalogs => Set<PropertyItemCatalog>();
    public DbSet<PropertyCodeCounter> PropertyCodeCounters => Set<PropertyCodeCounter>();
    public DbSet<AssetRegistry> AssetRegistries => Set<AssetRegistry>();
    public DbSet<PropertyAccountability> PropertyAccountabilities => Set<PropertyAccountability>();
    public DbSet<PropertyIssuanceReport> PropertyIssuanceReports => Set<PropertyIssuanceReport>();
    public DbSet<PhysicalCountSession> PhysicalCountSessions => Set<PhysicalCountSession>();
    public DbSet<PropertyIncidentReport> PropertyIncidentReports => Set<PropertyIncidentReport>();
    public DbSet<UnserviceablePropertyReport> UnserviceablePropertyReports => Set<UnserviceablePropertyReport>();
    public DbSet<ReceivingReport> ReceivingReports => Set<ReceivingReport>();

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
    }
}
