using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.MasterData.Data.Configurations;

public sealed class FundClusterConfiguration : IEntityTypeConfiguration<FundCluster>
{
    public void Configure(EntityTypeBuilder<FundCluster> builder)
    {
        builder.ToTable("FundClusters", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
