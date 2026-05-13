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
using AMIS.Modules.AssetRegister.Domain.Receiving;
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

