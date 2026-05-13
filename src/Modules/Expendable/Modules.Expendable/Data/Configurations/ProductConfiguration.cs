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

        builder.Property(p => p.SKU)
            .HasMaxLength(50)
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
            .HasMaxLength(10_000_000);  // Support base64-encoded images (up to ~7.6MB images)

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.Version)
            .IsRowVersion();

        // Self-reference: a product may have a parent product and many variants
        builder.HasOne(p => p.ParentProduct)
            .WithMany(p => p.Variants)
            .HasForeignKey(p => p.ParentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.SKU })
            .IsUnique();

        builder.HasIndex(p => new { p.TenantId, p.Status });

        // Soft Delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


