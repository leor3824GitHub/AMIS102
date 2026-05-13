using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class SMIRItemConfiguration : IEntityTypeConfiguration<SMIRItem>
{
    public void Configure(EntityTypeBuilder<SMIRItem> builder)
    {
        builder.ToTable("SMIRItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SMIRId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AssetTypeAtTimeOfIssuance).HasConversion<string>().HasMaxLength(8).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.SMIRId });
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryItemId });
        builder.HasIndex(x => x.SMIRId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}

