using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class AssetRegistryConfiguration : IEntityTypeConfiguration<AssetRegistry>
{
    public void Configure(EntityTypeBuilder<AssetRegistry> builder)
    {
        builder.ToTable("AssetRegistry", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemId).IsRequired();
        builder.Property(x => x.PropertyNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AssetType).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.LifecycleState).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CurrentPropertyStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.PropertyNo }).IsUnique();
        builder.HasIndex(x => x.TangibleInventoryItemId).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.LifecycleState });
        builder.HasIndex(x => new { x.TenantId, x.CurrentCustodianId });
        builder.HasIndex(x => new { x.TenantId, x.CurrentLocationId });

        builder.HasOne<TangibleInventoryItem>()
            .WithMany()
            .HasForeignKey(x => x.TangibleInventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyItemCatalog>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.CurrentLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}
