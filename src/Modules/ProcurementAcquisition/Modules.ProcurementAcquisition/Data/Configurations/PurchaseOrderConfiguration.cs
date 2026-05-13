using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PoNumber).HasMaxLength(32).IsRequired();
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
        // Version column kept for future xmin-based concurrency; not active until properly wired

        builder.HasIndex(x => new { x.TenantId, x.PoNumber }).IsUnique();
        builder.HasIndex(x => x.PurchaseRequestId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.OwnsMany(x => x.LineItems, b =>
        {
            b.ToJson();
            b.Property(li => li.ItemNo).IsRequired();
            b.Property(li => li.StockNumber).HasMaxLength(64);
            b.Property(li => li.Unit).HasMaxLength(64).IsRequired();
            b.Property(li => li.Description).HasMaxLength(500).IsRequired();
            b.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.UnitCost).HasPrecision(18, 4).IsRequired();
        });
    }
}

