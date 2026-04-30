using FSH.Modules.AssetProcurement.Domain.AssetPurchaseRequests;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetProcurement.Data.Configurations;

internal sealed class AssetPurchaseRequestConfiguration : IEntityTypeConfiguration<AssetPurchaseRequest>
{
    public void Configure(EntityTypeBuilder<AssetPurchaseRequest> builder)
    {
        builder.ToTable("AssetPurchaseRequests", AssetProcurementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PrNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Section).HasMaxLength(200);
        builder.Property(x => x.Justification).HasMaxLength(1000);
        builder.Property(x => x.SaiNumber).HasMaxLength(64);
        builder.Property(x => x.AlobsNumber).HasMaxLength(64);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsRowVersion();

        builder.OwnsMany(x => x.LineItems, li =>
        {
            li.ToJson();
            li.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
            li.Property(x => x.TechnicalSpecifications).HasMaxLength(1000);
            li.Property(x => x.Brand).HasMaxLength(200);
            li.Property(x => x.Model).HasMaxLength(200);
            li.Property(x => x.PropertyClassHint).HasMaxLength(64);
            li.Property(x => x.Unit).IsRequired().HasMaxLength(64);
        });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.PrNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.DepartmentId });
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
