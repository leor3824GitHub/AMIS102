using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Domain;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.BudgetDisbursement.Data;

public class BudgetDisbursementDbContext : BaseDbContext
{
    public DbSet<DisbursementVoucher> DisbursementVouchers => Set<DisbursementVoucher>();
    public DbSet<BudgetUtilizationRequest> BudgetUtilizationRequests => Set<BudgetUtilizationRequest>();
    public DbSet<BudgetDisbursementModuleSettings> BudgetDisbursementSettings => Set<BudgetDisbursementModuleSettings>();

    // Shared monotonic document-number counters (DV/BUR) — one table, keyed by SequenceKey. Global (not
    // tenant-scoped): DV/BUR numbers are year-only and unique across the whole system.
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    public BudgetDisbursementDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<BudgetDisbursementDbContext> options,
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDisbursementDbContext).Assembly);

        // NumberSequence lives in the framework assembly, so it isn't picked up by the assembly scan above.
        // multiTenant: false — DV/BUR series are global (year-keyed), matching the global unique index on
        // DvNumber/BurNumber. Rows store TenantId = "".
        modelBuilder.ConfigureNumberSequences(BudgetDisbursementModuleConstants.SchemaName, multiTenant: false);
    }
}

