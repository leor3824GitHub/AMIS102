using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Finance.Data.Configurations;

public sealed class BudgetUtilizationRecordConfiguration : IEntityTypeConfiguration<BudgetUtilizationRecord>
{
    public void Configure(EntityTypeBuilder<BudgetUtilizationRecord> builder)
    {
        builder.ToTable("BudgetUtilizationRecords", FinanceModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.BurNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisbursementVoucherNumber).HasMaxLength(32);
        builder.Property(x => x.AllotmentClass).HasMaxLength(16).IsRequired();
        builder.Property(x => x.UacsObjectCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResponsibilityCenter).HasMaxLength(32);
        builder.Property(x => x.Particulars).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.BurNumber).IsUnique();
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.DisbursementVoucherId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Version).IsRowVersion();
    }
}

