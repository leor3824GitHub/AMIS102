using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MasterData.Data.Configurations;

public sealed class PropertyClassItemConfiguration : IEntityTypeConfiguration<PropertyClassItem>
{
    public void Configure(EntityTypeBuilder<PropertyClassItem> builder)
    {
        builder.ToTable("PropertyClassItems", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PropertyClassId).IsRequired();
        builder.Property(x => x.ClassCode).HasMaxLength(4).IsRequired();
        builder.Property(x => x.ItemCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        // Unique: one item code per parent class
        builder.HasIndex(x => new { x.PropertyClassId, x.ItemCode }).IsUnique();
        builder.HasIndex(x => new { x.ClassCode, x.ItemCode }).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
