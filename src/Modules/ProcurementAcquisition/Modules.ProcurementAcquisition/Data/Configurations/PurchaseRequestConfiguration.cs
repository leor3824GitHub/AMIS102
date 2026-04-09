using FSH.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests", ProcurementAcquisitionModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.PrNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SaiNumber).HasMaxLength(64);
        builder.Property(x => x.AlobsNumber).HasMaxLength(64);
        builder.Property(x => x.Section).HasMaxLength(160);
        builder.Property(x => x.Purpose).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.PrType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        // Version column kept for future xmin-based concurrency; not active until properly wired

        builder.HasIndex(x => x.PrNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DepartmentId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.OwnsMany(x => x.LineItems, b =>
        {
            b.ToJson();
            b.Property(li => li.ItemNo).IsRequired();
            b.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.UnitOfIssue).HasMaxLength(64).IsRequired();
            b.Property(li => li.ItemDescription).HasMaxLength(500).IsRequired();
            b.Property(li => li.EstimatedUnitCost).HasPrecision(18, 4).IsRequired();
        });
    }
}
