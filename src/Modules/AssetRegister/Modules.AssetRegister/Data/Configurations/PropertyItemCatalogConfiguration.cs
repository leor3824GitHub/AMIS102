using FSH.Modules.AssetRegister.Domain.Catalog;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyItemCatalogConfiguration : IEntityTypeConfiguration<PropertyItemCatalog>
{
    public void Configure(EntityTypeBuilder<PropertyItemCatalog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyItemCatalog", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DefaultPropertyClass).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DefaultCategoryCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DefaultUnit).IsRequired().HasMaxLength(64);
        builder.Property(x => x.UacsObjectCode).HasMaxLength(32);

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}
