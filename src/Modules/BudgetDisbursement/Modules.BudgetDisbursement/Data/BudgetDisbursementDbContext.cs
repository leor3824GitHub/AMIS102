using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Persistence.Context;
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
    public DbSet<Domain.SignedDocuments.SignedDocument> SignedDocuments => Set<Domain.SignedDocuments.SignedDocument>();
    public DbSet<DvNumberSequence> DvNumberSequences => Set<DvNumberSequence>();
    public DbSet<BurNumberSequence> BurNumberSequences => Set<BurNumberSequence>();
    public DbSet<BudgetDisbursementModuleSettings> BudgetDisbursementSettings => Set<BudgetDisbursementModuleSettings>();

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
    }
}

