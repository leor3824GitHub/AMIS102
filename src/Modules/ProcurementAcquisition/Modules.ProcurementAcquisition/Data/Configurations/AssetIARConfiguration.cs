using AMIS.Modules.ProcurementAcquisition.Domain.AssetInspectionAcceptanceReports;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

internal sealed class AssetIARConfiguration : IEntityTypeConfiguration<AssetInspectionAcceptanceReport>
{
    public void Configure(EntityTypeBuilder<AssetInspectionAcceptanceReport> builder)
    {
        builder.ToTable("AssetIARs", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IarNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SupplierName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DeliveryReceiptNo).HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        // Version column kept for future xmin-based concurrency; not active until properly wired

        builder.Property(x => x.SubmittedForInspectionOnUtc);
        builder.Property(x => x.InspectedOnUtc);
        builder.Property(x => x.AcceptedOnUtc);
        builder.Property(x => x.CancelledOnUtc);

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
            li.Property(x => x.InspectionResult).HasConversion<int>();
        });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.IarNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.PurchaseOrderId });
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
