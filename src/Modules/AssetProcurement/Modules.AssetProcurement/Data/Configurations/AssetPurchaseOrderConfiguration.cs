using FSH.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetProcurement.Data.Configurations;

internal sealed class AssetPurchaseOrderConfiguration : IEntityTypeConfiguration<AssetPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<AssetPurchaseOrder> builder)
    {
        builder.ToTable("AssetPurchaseOrders", AssetProcurementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
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

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.PoNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PurchaseRequestId);
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
