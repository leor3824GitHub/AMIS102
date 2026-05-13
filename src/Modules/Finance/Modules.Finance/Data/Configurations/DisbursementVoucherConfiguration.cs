using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Finance.Data.Configurations;

public sealed class DisbursementVoucherConfiguration : IEntityTypeConfiguration<DisbursementVoucher>
{
    public void Configure(EntityTypeBuilder<DisbursementVoucher> builder)
    {
        builder.ToTable("DisbursementVouchers", FinanceModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DvNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Payee).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TinNo).HasMaxLength(32);
        builder.Property(x => x.PayeeAddress).HasMaxLength(500);
        builder.Property(x => x.Particulars).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ModeOfPayment).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => x.DvNumber).IsUnique();
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Version).IsRowVersion();
    }
}

