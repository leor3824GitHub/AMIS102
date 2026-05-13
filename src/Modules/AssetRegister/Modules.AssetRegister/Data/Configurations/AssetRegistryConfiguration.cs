using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class AssetRegistryConfiguration : IEntityTypeConfiguration<AssetRegistry>
{
    public void Configure(EntityTypeBuilder<AssetRegistry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AssetRegistries", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).IsRowVersion();

        // PropertyNumber VO → string column
        builder.Property(x => x.PropertyNo)
            .HasConversion(pn => pn.Value, s => PropertyNumber.Parse(s))
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("PropertyNo");

        builder.Property(x => x.PropertyClass).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CategoryCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SerialNo).HasMaxLength(200);
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.UacsObjectCode).IsRequired().HasMaxLength(32);

        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedImpairmentLosses).HasPrecision(18, 2);

        builder.Ignore(x => x.CarryingAmount);

        builder.HasIndex(x => new { x.TenantId, PropertyNo = x.PropertyNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.LifecycleState });
        builder.HasIndex(x => new { x.TenantId, x.ItemId });
        builder.HasIndex(x => new { x.TenantId, x.CurrentCustodianId });
    }
}

