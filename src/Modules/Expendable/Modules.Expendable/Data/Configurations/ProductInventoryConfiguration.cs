using AMIS.Modules.Expendable.Domain.Warehouse;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

/// <summary>
/// Configuration for ProductInventory aggregate - central warehouse stock ledger.
/// Each product maintains separate inventory per warehouse location.
/// Inventory batches are stored as JSON-owned collection for receipt traceability.
/// </summary>
public class ProductInventoryConfiguration : IEntityTypeConfiguration<ProductInventory>
{
    public void Configure(EntityTypeBuilder<ProductInventory> builder)
    {
        builder.ToTable($"{nameof(ProductInventory)}", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

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

        builder.Property(p => p.QuantityAvailable)
            .IsRequired();

        builder.Property(p => p.QuantityReserved)
            .IsRequired();

        builder.Property(p => p.QuantityIssued)
            .IsRequired();

        builder.Property(p => p.TotalValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        // Batches (Owned Collection stored as JSON)
        builder.OwnsMany(p => p.Batches, ob =>
        {
            ob.ToJson("Batches");
            ob.Property(x => x.PurchaseId).IsRequired();
            ob.Property(x => x.ProductId).IsRequired();
            ob.Property(x => x.UnitPrice).HasPrecision(18, 2);
            ob.Property(x => x.QuantityAvailable).IsRequired();
            ob.Property(x => x.QuantityIssued).IsRequired();
            ob.Property(x => x.ReceivedDate).IsRequired();
            ob.Property(x => x.Version).IsRequired();
        });

        // Indexes
        // Unique per-product per-warehouse per-tenant inventory
        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.WarehouseLocationId })
            .IsUnique();

        // Query efficiency for warehouse stock lookups
        builder.HasIndex(p => new { p.TenantId, p.WarehouseLocationId });

        // Query efficiency for product lookups across warehouses
        builder.HasIndex(p => new { p.TenantId, p.ProductId });

        // Query efficiency for available stock lookups
        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


