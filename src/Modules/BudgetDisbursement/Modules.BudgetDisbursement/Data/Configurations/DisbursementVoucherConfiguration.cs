using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.BudgetDisbursement.Data.Configurations;

public sealed class DisbursementVoucherConfiguration : IEntityTypeConfiguration<DisbursementVoucher>
{
    public void Configure(EntityTypeBuilder<DisbursementVoucher> builder)
    {
        builder.ToTable("DisbursementVouchers", BudgetDisbursementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DvNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BurNumber).HasMaxLength(32).IsRequired();
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
        builder.HasIndex(x => x.BudgetUtilizationRequestId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Computed read-only projections — not persisted.
        builder.Ignore(x => x.TotalDeductions);
        builder.Ignore(x => x.AmountDue);

        // Configurable deduction lines owned by the voucher. Stored in a child table; always
        // eager-loaded with the aggregate (owned-type behaviour). The navigation binds to the
        // private _deductions backing field.
        builder.OwnsMany(x => x.Deductions, d =>
        {
            d.ToTable("DisbursementVoucherDeductions", BudgetDisbursementModuleConstants.SchemaName);
            d.WithOwner().HasForeignKey("DisbursementVoucherId");
            d.HasKey(x => x.Id);
            d.Property(x => x.Id).ValueGeneratedNever();
            d.Property(x => x.Name).HasMaxLength(128).IsRequired();
            d.Property(x => x.Type).HasConversion<string>().HasMaxLength(16).IsRequired();
            d.Property(x => x.Value).HasPrecision(18, 4).IsRequired();
        });
        builder.Navigation(x => x.Deductions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Client-supplied concurrency token. NOT IsRowVersion(): on PostgreSQL/Npgsql a rowversion
        // byte[] is treated as store-generated, so EF omits it on INSERT and the NOT NULL bytea
        // column is rejected. IsConcurrencyToken keeps the column non-generated so EF sends the value.
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

