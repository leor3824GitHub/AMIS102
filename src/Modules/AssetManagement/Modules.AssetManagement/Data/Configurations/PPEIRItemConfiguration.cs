using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetManagement.Data.Configurations;

public sealed class PPEIRItemConfiguration : IEntityTypeConfiguration<PPEIRItem>
{
    public void Configure(EntityTypeBuilder<PPEIRItem> builder)
    {
        builder.ToTable("PPEIRItems", AssetManagementModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PPEIRId).IsRequired();
        builder.Property(x => x.TangibleInventoryItemId).IsRequired();
        builder.Property(x => x.ItemNo).IsRequired();
        builder.Property(x => x.PropertyCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.PPESpecification).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DateAcquired).IsRequired();
        builder.Property(x => x.AcquisitionCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AccumulatedDepreciation).HasColumnType("numeric(18,2)");
        builder.Property(x => x.BookValue).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.PPEIRId });
        builder.HasIndex(x => new { x.TenantId, x.TangibleInventoryItemId });
        builder.HasIndex(x => x.PPEIRId);
        builder.HasIndex(x => x.TangibleInventoryItemId);
    }
}

