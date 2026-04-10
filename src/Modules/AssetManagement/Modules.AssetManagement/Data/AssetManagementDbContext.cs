using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.AssetManagement.Data;

public class AssetManagementDbContext : BaseDbContext
{
    public DbSet<SemiExpendableItem>              SemiExpendableItems              => Set<SemiExpendableItem>();
    public DbSet<SemiExpendableProperty>          SemiExpendableProperties         => Set<SemiExpendableProperty>();
    public DbSet<SuppliesMaterialsReceivingReport> SuppliesMaterialsReceivingReports => Set<SuppliesMaterialsReceivingReport>();
    public DbSet<SMRRItem>                        SMRRItems                        => Set<SMRRItem>();
    public DbSet<InventoryCustodianSlip>          InventoryCustodianSlips          => Set<InventoryCustodianSlip>();
    public DbSet<ICSItem>                         ICSItems                         => Set<ICSItem>();

    public AssetManagementDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<AssetManagementDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetManagementDbContext).Assembly);
    }
}
