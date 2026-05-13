using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class RRPItemConfiguration : IEntityTypeConfiguration<RRPItem>
{
    public void Configure(EntityTypeBuilder<RRPItem> builder)
    {
        builder.ToTable("RRPItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RRPId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.SourceDocumentRef).HasMaxLength(64);
        builder.Property(x => x.PropertyCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.RRPId });
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryItemId });
        builder.HasIndex(x => x.RRPId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}

