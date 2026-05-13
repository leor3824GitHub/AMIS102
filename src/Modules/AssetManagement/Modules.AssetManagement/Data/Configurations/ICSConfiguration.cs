using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class ICSConfiguration : IEntityTypeConfiguration<InventoryCustodianSlip>
{
    public void Configure(EntityTypeBuilder<InventoryCustodianSlip> builder)
    {
        builder.ToTable("InventoryCustodianSlips", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ICSNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(50);
        builder.Property(x => x.AssetType).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ExpiresOn);
        builder.Property(x => x.RenewedFromICSId);
        builder.Property(x => x.RenewedByICSId);
        builder.Property(x => x.CancelledByRRSPId);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.ICSNo }).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.IssuedFromEmployeeId);
        builder.HasIndex(x => x.ReceivedByEmployeeId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresOn);
        builder.HasIndex(x => x.AssetType);
        builder.HasIndex(x => x.RenewedFromICSId);
        builder.HasIndex(x => x.RenewedByICSId);
        builder.HasIndex(x => x.CancelledByRRSPId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}

