using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.MasterData.Data.Configurations;

public sealed class FundingSourceCodeConfiguration : IEntityTypeConfiguration<FundingSourceCode>
{
    public void Configure(EntityTypeBuilder<FundingSourceCode> builder)
    {
        builder.ToTable("FundingSourceCodes", MasterDataModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FundClusterCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.FinancingSource).HasMaxLength(250);
        builder.Property(x => x.Authorization).HasMaxLength(250);
        builder.Property(x => x.FundCategory).HasMaxLength(250);
        builder.Property(x => x.FundSubCategory).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DepartmentName).HasMaxLength(250);
        builder.Property(x => x.AgencyName).HasMaxLength(250);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.FundClusterCode);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
