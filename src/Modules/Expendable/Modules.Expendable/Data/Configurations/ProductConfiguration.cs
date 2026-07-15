using AMIS.Modules.Expendable.Domain.Products;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable($"{nameof(Product)}s", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.StockNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Article)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.UnitOfMeasure)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(1024);       // storage key of the full image (files, not base64)
        builder.Property(p => p.ThumbnailUrl)
            .HasMaxLength(1024);       // storage key of the list thumbnail

        builder.Property(p => p.Status)
            .HasConversion<int>();

        // Optimistic concurrency via Postgres system column xmin (mirrors AssetRegistry) —
        // avoids the bytea IsRowVersion() pitfall that never generates a value on insert.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Self-reference: a product may have a parent product and many variants
        builder.HasOne(p => p.ParentProduct)
            .WithMany(p => p.Variants)
            .HasForeignKey(p => p.ParentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.StockNo })
            .IsUnique();

        builder.HasIndex(p => new { p.TenantId, p.Status });

        // Composite indexes for the actual SearchProducts filter/sort shapes:
        // filters on CategoryId/SupplierId, default sort OrderBy(Name).
        builder.HasIndex(p => new { p.TenantId, p.CategoryId });
        builder.HasIndex(p => new { p.TenantId, p.SupplierId });
        builder.HasIndex(p => new { p.TenantId, p.Name });

        // Soft Delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


