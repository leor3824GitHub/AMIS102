using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PPEIRItemConfiguration : IEntityTypeConfiguration<PPEIRItem>
{
    public void Configure(EntityTypeBuilder<PPEIRItem> builder)
    {
        builder.ToTable("PPEIRItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PPEIRId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.PropertyCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.PPESpecification).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DateAcquired).IsRequired();
        builder.Property(x => x.AcquisitionCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AccumulatedDepreciation).HasColumnType("numeric(18,2)");
        builder.Property(x => x.BookValue).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => x.PPEIRId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}
