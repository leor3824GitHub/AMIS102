using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.MasterData.Data.Configurations;

public sealed class ModeOfProcurementConfiguration : IEntityTypeConfiguration<ModeOfProcurement>
{
    public void Configure(EntityTypeBuilder<ModeOfProcurement> builder)
    {
        builder.ToTable("ModesOfProcurement", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
