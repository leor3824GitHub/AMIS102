using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class TangibleItemConfiguration : IEntityTypeConfiguration<TangibleItem>
{
    public void Configure(EntityTypeBuilder<TangibleItem> builder)
    {
        builder.ToTable("TangibleItems", AssetManagementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PropertyNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PropertyClass).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CategoryCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AcquisitionDate).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.PropertyNo }).IsUnique();
        builder.HasIndex(x => x.ItemId);
        builder.HasIndex(x => new { x.TenantId, x.PropertyClass });
        builder.HasIndex(x => new { x.TenantId, x.CategoryCode });

        builder.Property(x => x.TangibleInventoryItemId);
        builder.HasIndex(x => x.TangibleInventoryItemId);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}
