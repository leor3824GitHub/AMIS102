using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class SemiExpendableItemConfiguration : IEntityTypeConfiguration<SemiExpendableItem>
{
    public void Configure(EntityTypeBuilder<SemiExpendableItem> builder)
    {
        builder.ToTable("SemiExpendableItems", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UACSObjectCode).HasMaxLength(32);
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EstimatedUsefulLifeYears);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Name);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
