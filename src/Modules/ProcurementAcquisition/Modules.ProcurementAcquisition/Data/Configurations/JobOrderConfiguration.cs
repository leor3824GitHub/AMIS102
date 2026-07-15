using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class JobOrderConfiguration : IEntityTypeConfiguration<JobOrder>
{
    public void Configure(EntityTypeBuilder<JobOrder> builder)
    {
        builder.ToTable("JobOrders", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.JoNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.JobRequestNo).HasMaxLength(64);
        builder.Property(x => x.RequisitioningOffice).HasMaxLength(256);
        builder.Property(x => x.SupplierName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SupplierAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SupplierTin).HasMaxLength(32);
        builder.Property(x => x.PlaceOfDelivery).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DeliveryTerm).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PaymentTerm).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FundCluster).HasMaxLength(100);
        builder.Property(x => x.OursBursNumber).HasMaxLength(100);
        builder.Property(x => x.ModeOfProcurement).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);

        // Funds Available — Accountant
        builder.Property(x => x.FundsAvailableCertifiedByName).HasMaxLength(200);
        builder.Property(x => x.FundsAvailableCertifiedByDesignation).HasMaxLength(200);
        // Approved — Authorized Official who issued the JO
        builder.Property(x => x.IssuedByName).HasMaxLength(200);
        builder.Property(x => x.IssuedByDesignation).HasMaxLength(200);
        // Inspection — C.O./F.O. Inspector (assigned at creation)
        builder.Property(x => x.InspectorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InspectorDesignation).HasMaxLength(200);
        builder.Property(x => x.InspectionInvoiceNo).HasMaxLength(64);
        builder.Property(x => x.InspectionFindings).HasMaxLength(1000);
        // Acceptance — Supply Officer (signatory sourced from the Organization Profile)
        builder.Property(x => x.AcceptanceInvoiceNo).HasMaxLength(64);
        builder.Property(x => x.PartialDeliveryNote).HasMaxLength(500);

        // PostgreSQL xmin system column — true optimistic concurrency, auto-updated by the DB on every UPDATE.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.JoNumber }).IsUnique();
        builder.HasIndex(x => x.PurchaseRequestId);
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.OwnsMany(x => x.LineItems, b =>
        {
            b.ToJson();
            b.Property(li => li.ItemNo).IsRequired();
            b.Property(li => li.Unit).HasMaxLength(64);
            b.Property(li => li.Description).HasMaxLength(500).IsRequired();
            b.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.UnitCost).HasPrecision(18, 4).IsRequired();
        });
    }
}
