using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PrNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SaiNumber).HasMaxLength(64);
        builder.Property(x => x.AlobsNumber).HasMaxLength(64);
        builder.Property(x => x.ResponsibilityCenterCode).HasMaxLength(160);
        builder.Property(x => x.Purpose).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Justification).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.PrType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.RequestedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ApprovedByName).HasMaxLength(200);
        // Version column kept for future xmin-based concurrency; not active until properly wired

        builder.HasIndex(x => new { x.TenantId, x.PrNumber }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DepartmentId);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

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

