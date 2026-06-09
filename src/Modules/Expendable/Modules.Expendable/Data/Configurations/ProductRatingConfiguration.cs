using AMIS.Modules.Expendable.Domain.Products;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

public class ProductRatingConfiguration : IEntityTypeConfiguration<ProductRating>
{
    public void Configure(EntityTypeBuilder<ProductRating> builder)
    {
        builder.ToTable($"{nameof(ProductRating)}s", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.RaterUserId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Value)
            .IsRequired();

        // One rating per user per product.
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.RaterUserId })
            .IsUnique();

        // Fast aggregation per product.
        builder.HasIndex(p => new { p.TenantId, p.ProductId });

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}
