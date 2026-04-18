using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PropertyIncidentItemConfiguration : IEntityTypeConfiguration<PropertyIncidentItem>
{
    public void Configure(EntityTypeBuilder<PropertyIncidentItem> builder)
    {
        builder.ToTable("PropertyIncidentItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AssetTypeAtTimeOfReport).HasConversion<string>().HasMaxLength(8).IsRequired();

        builder.HasIndex(x => x.ReportId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}
