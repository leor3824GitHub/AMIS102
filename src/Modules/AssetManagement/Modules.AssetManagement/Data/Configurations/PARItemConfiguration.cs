using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PARItemConfiguration : IEntityTypeConfiguration<PARItem>
{
    public void Configure(EntityTypeBuilder<PARItem> builder)
    {
        builder.ToTable("PARItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PARId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ItemDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.EstimatedUsefulLifeYears).IsRequired();
        builder.Property(x => x.DateAcquired).IsRequired();

        builder.HasIndex(x => x.PARId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}
