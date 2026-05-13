using AMIS.Modules.Expendable.Domain.Inventory;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

/// <summary>
/// Configuration for RejectedInventory aggregate - failed QC inventory tracking.
/// Tracks rejected items awaiting return to supplier or disposal.
/// Seperate lifecycle from ProductInventory and SupplyRequest flow.
/// </summary>
public class RejectedInventoryConfiguration : IEntityTypeConfiguration<RejectedInventory>
{
    public void Configure(EntityTypeBuilder<RejectedInventory> builder)
    {
        builder.ToTable($"{nameof(RejectedInventory)}", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PurchaseId)
            .IsRequired();

        builder.Property(p => p.PurchaseInspectionId);

        builder.Property(p => p.ProductId)
            .IsRequired();

        builder.Property(p => p.ProductCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.WarehouseLocationId)
            .IsRequired();

        builder.Property(p => p.WarehouseLocationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.QuantityRejected)
            .IsRequired();

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        // Computed in domain from QuantityRejected * UnitPrice; not persisted directly.
        builder.Ignore(p => p.TotalValue);

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.DispositionDate);

        builder.Property(p => p.DispositionNotes)
            .HasMaxLength(1000);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        // Indexes
        // Foreign key lookups
        builder.HasIndex(p => new { p.TenantId, p.PurchaseId });
        builder.HasIndex(p => new { p.TenantId, p.PurchaseInspectionId });

        // Query rejected inventory by warehouse
        builder.HasIndex(p => new { p.TenantId, p.WarehouseLocationId });

        // Query pending return/disposal
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


