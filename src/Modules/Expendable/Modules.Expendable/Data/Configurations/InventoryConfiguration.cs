using AMIS.Modules.Expendable.Domain.Inventory;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

public class EmployeeInventoryConfiguration : IEntityTypeConfiguration<EmployeeInventory>
{
    public void Configure(EntityTypeBuilder<EmployeeInventory> builder)
    {
        builder.ToTable($"{nameof(EmployeeInventory)}", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.EmployeeId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        // Batches (Owned Type)
        builder.OwnsMany(p => p.Batches, ob =>
        {
            ob.ToTable($"{nameof(InventoryBatch)}es", ExpendableModuleConstants.SchemaName);
            ob.HasKey(x => x.Id);
            ob.Property(x => x.BatchNumber).HasMaxLength(50);
        });

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId, p.ProductId })
            .IsUnique();

    }
}

public class InventoryConsumptionConfiguration : IEntityTypeConfiguration<InventoryConsumption>
{
    public void Configure(EntityTypeBuilder<InventoryConsumption> builder)
    {
        builder.ToTable($"{nameof(InventoryConsumption)}s", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.EmployeeId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Reason)
            .HasMaxLength(500);

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.EmployeeId });

        builder.HasIndex(p => new { p.TenantId, p.EmployeeInventoryId });

        builder.HasIndex(p => new { p.TenantId, p.ProductId });

    }
}


