using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class ICSConfiguration : IEntityTypeConfiguration<InventoryCustodianSlip>
{
    public void Configure(EntityTypeBuilder<InventoryCustodianSlip> builder)
    {
        builder.ToTable("InventoryCustodianSlips", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ICSNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.ICSNo).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.IssuedFromEmployeeId);
        builder.HasIndex(x => x.ReceivedByEmployeeId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
