using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class TangibleInventoryItemConfiguration : IEntityTypeConfiguration<TangibleInventoryItem>
{
    public void Configure(EntityTypeBuilder<TangibleInventoryItem> builder)
    {
        builder.ToTable("TangibleInventoryItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TangibleInventoryId).IsRequired();
        builder.Property(x => x.TangibleItemId).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.AssetType).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(x => x.ThresholdAmountUsed).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.IsIssued).HasDefaultValue(false).IsRequired();

        // Snapshot fields
        builder.Property(x => x.PropertyNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ItemId).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryId });
        builder.HasIndex(x => new { x.TenantId, x.ItemId });
        builder.HasIndex(x => new { x.TenantId, x.AssetType });
        builder.HasIndex(x => new { x.TenantId, x.IsIssued });
        builder.HasIndex(x => x.TangibleInventoryId);
        builder.HasIndex(x => x.TangibleItemId).IsUnique(); // one item per report
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => x.AssetType);
        builder.HasIndex(x => x.IsIssued);

        builder.HasOne<TangibleInventory>()
            .WithMany()
            .HasForeignKey(x => x.TangibleInventoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TangibleItem>()
            .WithMany()
            .HasForeignKey(x => x.TangibleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PropertyItemCatalog>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

