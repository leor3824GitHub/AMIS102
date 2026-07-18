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

        builder.Property(p => p.WarehouseLocationId)
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

        // Optimistic concurrency via the Postgres system column xmin (mirrors AssetRegistry/Product) —
        // avoids the bytea IsRowVersion() pitfall and needs no domain-managed token field.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Batches — receipt records in a relational child table (was a JSON-owned collection). Promoting them
        // off the JSON column ends the whole-document rewrite on every reserve/issue/cancel and lets the Stock
        // Card read receipts via a join instead of deserializing an ever-growing blob. As an owned collection
        // it is still auto-loaded with the aggregate and lives under the owner's tenant + soft-delete scope.
        builder.OwnsMany(p => p.Batches, ob =>
        {
            ob.ToTable("ProductInventoryBatches", ExpendableModuleConstants.SchemaName);
            ob.WithOwner().HasForeignKey("ProductInventoryId");
            ob.HasKey(x => x.Id);   // explicit Guid PK (domain-assigned) — avoids the implicit int shadow key
            ob.Property(x => x.PurchaseId).IsRequired();
            ob.Property(x => x.UnitPrice).HasPrecision(18, 2);
            ob.Property(x => x.QuantityAvailable).IsRequired();
            ob.Property(x => x.SourceReference).HasMaxLength(64);  // IAR number shown as the Stock Card reference
            ob.Property(x => x.ReceivedDate).IsRequired();
            ob.HasIndex("ProductInventoryId");  // batch lookups by parent (Stock Card)
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


