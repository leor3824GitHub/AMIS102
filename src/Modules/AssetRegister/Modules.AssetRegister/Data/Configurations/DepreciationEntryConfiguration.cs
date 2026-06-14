using AMIS.Modules.AssetRegister.Domain.Assets;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class DepreciationEntryConfiguration : IEntityTypeConfiguration<DepreciationEntry>
{
    public void Configure(EntityTypeBuilder<DepreciationEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("DepreciationEntries", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciationAfter).HasPrecision(18, 2);
        builder.Property(x => x.CarryingAmountAfter).HasPrecision(18, 2);

        // One posting per asset per month — keeps catch-up runs idempotent.
        builder.HasIndex(x => new { x.TenantId, x.AssetRegistryId, x.Period }).IsUnique();
    }
}
