using AMIS.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetProcurement.Data.Configurations;

internal sealed class AssetPurchaseOrderConfiguration : IEntityTypeConfiguration<AssetPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<AssetPurchaseOrder> builder)
    {
        builder.ToTable("AssetPurchaseOrders", AssetProcurementModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PoNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SupplierName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SupplierAddress).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SupplierTin).HasMaxLength(32);
        builder.Property(x => x.PlaceOfDelivery).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DeliveryTerm).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PaymentTerm).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FundCluster).HasMaxLength(64);
        builder.Property(x => x.OblRequestNumber).HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsRowVersion();

        builder.OwnsMany(x => x.LineItems, li =>
        {
            li.ToJson();
            li.Property(x => x.Unit).IsRequired().HasMaxLength(64);
            li.Property(x => x.Description).IsRequired().HasMaxLength(500);
            li.Property(x => x.TechnicalSpecifications).HasMaxLength(1000);
            li.Property(x => x.Brand).HasMaxLength(200);
            li.Property(x => x.Model).HasMaxLength(200);
            li.Property(x => x.PropertyClassHint).HasMaxLength(64);
        });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.PoNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.PurchaseRequestId });
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}

