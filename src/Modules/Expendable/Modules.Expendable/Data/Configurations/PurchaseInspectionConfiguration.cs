using AMIS.Modules.Expendable.Domain.Purchases;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

/// <summary>
/// Configuration for PurchaseInspection aggregate - quality control gate.
/// Independent from Purchase entity; inspection records track QA acceptance/rejection.
/// InspectionDefects are stored as JSON-owned collection for detailed defect tracking.
/// </summary>
public class PurchaseInspectionConfiguration : IEntityTypeConfiguration<PurchaseInspection>
{
    public void Configure(EntityTypeBuilder<PurchaseInspection> builder)
    {
        builder.ToTable($"{nameof(PurchaseInspection)}", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PurchaseId)
            .IsRequired();

        builder.Property(p => p.ProductId)
            .IsRequired();

        builder.Property(p => p.QuantityReceivedForInspection)
            .IsRequired();

        builder.Property(p => p.QuantityAccepted)
            .IsRequired();

        builder.Property(p => p.QuantityRejected)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.InspectedBy)
            .IsRequired();

        builder.Property(p => p.InspectionDate);

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(500);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        // Defects (Owned Collection stored as JSON)
        builder.OwnsMany(p => p.Defects, ob =>
        {
            ob.ToJson("Defects");
            ob.Property(x => x.UnitNumber).IsRequired();
            ob.Property(x => x.DefectDescription).HasMaxLength(500);
            ob.Property(x => x.Severity).HasMaxLength(50);
        });

        // Indexes
        // Foreign key lookup efficiency
        builder.HasIndex(p => new { p.TenantId, p.PurchaseId });

        // Query pending inspections
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


