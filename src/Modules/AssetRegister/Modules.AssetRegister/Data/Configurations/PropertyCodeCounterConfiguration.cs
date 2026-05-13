using AMIS.Modules.AssetRegister.Domain.Catalog;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyCodeCounterConfiguration : IEntityTypeConfiguration<PropertyCodeCounter>
{
    public void Configure(EntityTypeBuilder<PropertyCodeCounter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyCodeCounters", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CounterKey).IsRequired().HasMaxLength(32);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TenantId, x.Year, x.Month, x.CounterKey }).IsUnique();
    }
}

