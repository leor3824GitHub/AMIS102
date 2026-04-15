using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PropertyAcknowledgementReceiptConfiguration : IEntityTypeConfiguration<PropertyAcknowledgementReceipt>
{
    public void Configure(EntityTypeBuilder<PropertyAcknowledgementReceipt> builder)
    {
        builder.ToTable("PropertyAcknowledgementReceipts", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PARNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.PARType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReceivedFromEmployeeId).IsRequired();
        builder.Property(x => x.ReceivedByEmployeeId).IsRequired();
        builder.Property(x => x.ApprovedByEmployeeId).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.PARNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.ReceivedByEmployeeId);
        builder.HasIndex(x => x.PARType);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
