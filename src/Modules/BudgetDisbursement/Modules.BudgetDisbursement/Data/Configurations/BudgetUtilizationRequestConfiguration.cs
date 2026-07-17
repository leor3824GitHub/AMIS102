using AMIS.Framework.Persistence.SignedDocuments;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.BudgetDisbursement.Data.Configurations;

public sealed class BudgetUtilizationRequestConfiguration : IEntityTypeConfiguration<BudgetUtilizationRequest>
{
    public void Configure(EntityTypeBuilder<BudgetUtilizationRequest> builder)
    {
        builder.ToTable("BudgetUtilizationRequests", BudgetDisbursementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.ConfigureSignedCopy(x => x.SignedCopy);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BurNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisbursementVoucherNumber).HasMaxLength(32);
        // Stores the fund cluster code (e.g. "01"), sourced from the Fund Cluster master data.
        builder.Property(x => x.FundCluster).HasMaxLength(16).IsRequired();
        // Stores the standard allotment-class code (PS / MOOE / FE / CO), not the full label.
        builder.Property(x => x.AllotmentClass).HasMaxLength(16).IsRequired();
        builder.Property(x => x.UacsObjectCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResponsibilityCenter).HasMaxLength(32);
        builder.Property(x => x.Particulars).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.BurNumber).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.DisbursementVoucherId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        // Named filter form is required: .IsMultiTenant() registers a named query filter, and EF Core 10
        // forbids mixing an anonymous filter with named ones on the same entity.
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        // Client-supplied concurrency token. NOT IsRowVersion(): on PostgreSQL/Npgsql a rowversion
        // byte[] is treated as store-generated, so EF omits it on INSERT and the NOT NULL bytea
        // column is rejected. IsConcurrencyToken keeps the column non-generated so EF sends the value.
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

