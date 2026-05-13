using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class RRSPItemConfiguration : IEntityTypeConfiguration<RRSPItem>
{
    public void Configure(EntityTypeBuilder<RRSPItem> builder)
    {
        builder.ToTable("RRSPItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RRSPId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AssetTypeAtTimeOfReturn).HasConversion<string>().HasMaxLength(8).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.RRSPId });
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryItemId });
        builder.HasIndex(x => x.RRSPId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}

