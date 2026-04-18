using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MasterData.Data.Configurations;

public sealed class CapitalizationThresholdConfiguration : IEntityTypeConfiguration<CapitalizationThreshold>
{
    public void Configure(EntityTypeBuilder<CapitalizationThreshold> builder)
    {
        builder.ToTable("CapitalizationThresholds", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CircularName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CapitalizationAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.SemiExpendableLowValueThreshold).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.EffectivityDate).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.IsActive);
    }
}
