using FSH.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetProcurement.Data.Configurations;

internal sealed class AssetIARConfiguration : IEntityTypeConfiguration<AssetInspectionAcceptanceReport>
{
    public void Configure(EntityTypeBuilder<AssetInspectionAcceptanceReport> builder)
    {
        builder.ToTable("AssetIARs", AssetProcurementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.IarNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SupplierName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DeliveryReceiptNo).HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsRowVersion();

        builder.OwnsMany(x => x.LineItems, li =>
        {
            li.ToJson();
            li.Property(x => x.Description).IsRequired().HasMaxLength(500);
            li.Property(x => x.TechnicalSpecifications).HasMaxLength(1000);
            li.Property(x => x.Brand).HasMaxLength(200);
            li.Property(x => x.Model).HasMaxLength(200);
            li.Property(x => x.SerialNo).HasMaxLength(200);
            li.Property(x => x.PropertyClassHint).HasMaxLength(64);
            li.Property(x => x.Unit).IsRequired().HasMaxLength(64);
            li.Property(x => x.InspectionRemarks).HasMaxLength(500);
        });

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.IarNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
