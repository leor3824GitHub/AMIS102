using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SMRRConfiguration : IEntityTypeConfiguration<SuppliesMaterialsReceivingReport>
{
    public void Configure(EntityTypeBuilder<SuppliesMaterialsReceivingReport> builder)
    {
        builder.ToTable("SMRRs", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SMRRNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ReceivedFrom).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.ReceiptType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.OtherReceiptType).HasMaxLength(100);
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.ReceivedByEmployeeId);
        builder.HasIndex(x => x.ReceivedByEmployeeId);
        builder.Property(x => x.NotedByEmployeeId);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.SMRRNo }).IsUnique();
        builder.HasIndex(x => x.Date);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}
