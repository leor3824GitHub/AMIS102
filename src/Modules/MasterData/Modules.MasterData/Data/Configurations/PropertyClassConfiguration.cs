using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MasterData.Data.Configurations;

public sealed class PropertyClassConfiguration : IEntityTypeConfiguration<PropertyClass>
{
    public void Configure(EntityTypeBuilder<PropertyClass> builder)
    {
        builder.ToTable("PropertyClasses", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(4).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.PropertyClass)
            .HasForeignKey(x => x.PropertyClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
