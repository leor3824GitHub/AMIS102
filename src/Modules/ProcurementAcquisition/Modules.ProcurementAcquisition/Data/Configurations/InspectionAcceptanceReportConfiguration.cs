using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

internal sealed class InspectionAcceptanceReportConfiguration : IEntityTypeConfiguration<InspectionAcceptanceReport>
{
    public void Configure(EntityTypeBuilder<InspectionAcceptanceReport> builder)
    {
        builder.ToTable("InspectionAcceptanceReports", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IarNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SupplierName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.DeliveryReceiptNo).HasMaxLength(64);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.Category).IsRequired().HasDefaultValue(Contracts.v1.PurchaseRequests.ProcurementCategory.Asset);

        builder.Property(x => x.SubmittedForInspectionOnUtc);
        builder.Property(x => x.InspectedOnUtc);
        builder.Property(x => x.AcceptedOnUtc);
        builder.Property(x => x.CancelledOnUtc);

        builder.Property(x => x.AcceptedByName).HasMaxLength(200);
        builder.Property(x => x.AcceptedByDesignation).HasMaxLength(200);

        // PostgreSQL xmin system column — true optimistic concurrency, auto-updated by the DB on every UPDATE.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

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
            li.Property(x => x.UacsObjectCode).HasMaxLength(64); // copied from PO line snapshot
            li.Property(x => x.StockPropertyNo).HasMaxLength(64);
            li.Property(x => x.StockNumber).HasMaxLength(64);     // Supply IARs: product StockNo (JSON blob)
        });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.IarNumber }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.PurchaseOrderId });
        builder.HasIndex(x => x.CreatedOnUtc);
    }
}
