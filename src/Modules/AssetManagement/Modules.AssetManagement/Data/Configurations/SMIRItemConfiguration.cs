using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SMIRItemConfiguration : IEntityTypeConfiguration<SMIRItem>
{
    public void Configure(EntityTypeBuilder<SMIRItem> builder)
    {
        builder.ToTable("SMIRItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SMIRId).IsRequired();
        builder.Property(x => x.SemiExpendablePropertyId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.CategoryAtTimeOfIssuance).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(x => x.SMIRId);
        builder.HasIndex(x => x.SemiExpendablePropertyId);
    }
}
