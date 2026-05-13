using AMIS.Modules.Expendable.Domain.Purchases;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Expendable.Data.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable($"{nameof(Purchase)}s", ExpendableModuleConstants.SchemaName)
            .IsMultiTenant();

        // Primary Key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.TenantId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PurchaseOrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.SupplierId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.Property(p => p.ReceivingNotes)
            .HasMaxLength(1000);

        builder.Property(p => p.Version)
            .IsConcurrencyToken();

        // Line Items (Owned Type)
        builder.OwnsMany(p => p.LineItems, ob =>
        {
            ob.ToJson("LineItems");
            ob.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.PurchaseOrderNumber })
            .IsUnique();

        builder.HasIndex(p => new { p.TenantId, p.Status });

        builder.HasIndex(p => new { p.TenantId, p.SupplierId });

        // Soft Delete
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", p => !p.IsDeleted);
    }
}


