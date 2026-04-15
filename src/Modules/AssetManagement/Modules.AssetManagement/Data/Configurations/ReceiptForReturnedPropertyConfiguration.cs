using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class ReceiptForReturnedPropertyConfiguration : IEntityTypeConfiguration<ReceiptForReturnedProperty>
{
    public void Configure(EntityTypeBuilder<ReceiptForReturnedProperty> builder)
    {
        builder.ToTable("ReceiptForReturnedProperties", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RRSPNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.ICSId).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.RRSPNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.ICSId);
        builder.HasIndex(x => x.ReceivedByEmployeeId);
        builder.HasIndex(x => x.ReturnedByEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
