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
    public DbSet<CapitalizationThresholdPolicy>   CapitalizationThresholdPolicies  => Set<CapitalizationThresholdPolicy>();
    public DbSet<ReclassificationRecord>           ReclassificationRecords           => Set<ReclassificationRecord>();
    public DbSet<SemiExpendableIssuanceRecord>     SemiExpendableIssuanceRecords     => Set<SemiExpendableIssuanceRecord>();
    public DbSet<SMIRItem>                         SMIRItems                         => Set<SMIRItem>();
    public DbSet<SemiExpendableItem>              SemiExpendableItems              => Set<SemiExpendableItem>();
    public DbSet<SemiExpendableProperty>          SemiExpendableProperties         => Set<SemiExpendableProperty>();
    public DbSet<SuppliesMaterialsReceivingReport> SuppliesMaterialsReceivingReports => Set<SuppliesMaterialsReceivingReport>();
    public DbSet<SMRRItem>                        SMRRItems                        => Set<SMRRItem>();
    public DbSet<InventoryCustodianSlip>          InventoryCustodianSlips          => Set<InventoryCustodianSlip>();
    public DbSet<ICSItem>                         ICSItems                         => Set<ICSItem>();
    public DbSet<ReceiptForReturnedProperty>      ReceiptForReturnedProperties     => Set<ReceiptForReturnedProperty>();
    public DbSet<RRSPItem>                        RRSPItems                        => Set<RRSPItem>();
    public DbSet<PropertyIncidentReport>          PropertyIncidentReports          => Set<PropertyIncidentReport>();
    public DbSet<PropertyIncidentItem>            PropertyIncidentItems            => Set<PropertyIncidentItem>();
    public DbSet<UnserviceablePropertyReport>     UnserviceablePropertyReports     => Set<UnserviceablePropertyReport>();
    public DbSet<UnserviceablePropertyItem>       UnserviceablePropertyItems       => Set<UnserviceablePropertyItem>();

    // PPE track
    public DbSet<PPEItem>                         PPEItems                         => Set<PPEItem>();
    public DbSet<PPEReceivingReport>              PPEReceivingReports              => Set<PPEReceivingReport>();
    public DbSet<PPERRItem>                       PPERRItems                       => Set<PPERRItem>();
    public DbSet<PropertyAcknowledgementReceipt>  PropertyAcknowledgementReceipts  => Set<PropertyAcknowledgementReceipt>();
    public DbSet<PARItem>                         PARItems                         => Set<PARItem>();
    public DbSet<PPEIssuanceReport>               PPEIssuanceReports               => Set<PPEIssuanceReport>();
    public DbSet<PPEIRItem>                       PPEIRItems                       => Set<PPEIRItem>();
    public DbSet<ReceiptForReturnedPPE>           ReceiptsForReturnedPPE           => Set<ReceiptForReturnedPPE>();
    public DbSet<RRPItem>                         RRPItems                         => Set<RRPItem>();

    // Physical Count track
    public DbSet<PhysicalCountSession>            PhysicalCountSessions            => Set<PhysicalCountSession>();
    public DbSet<PhysicalCountEntry>              PhysicalCountEntries             => Set<PhysicalCountEntry>();

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
