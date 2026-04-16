using FSH.Modules.AssetManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.AssetManagement.Data.Configurations;

public sealed class PropertyCodeCounterConfiguration : IEntityTypeConfiguration<PropertyCodeCounter>
{
    public void Configure(EntityTypeBuilder<PropertyCodeCounter> builder)
    {
        builder.ToTable("PropertyCodeCounters", AssetManagementModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClassCode).HasMaxLength(4).IsRequired();
        builder.Property(x => x.ItemCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.LastSequence).IsRequired().HasDefaultValue(0);

        // PostgreSQL xmin is a system column tracking the transaction that last modified the row.
        // It updates automatically on every UPDATE, giving true optimistic concurrency without a separate column.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // One counter row per (TenantId + ClassCode + ItemCode + Year)
        builder.HasIndex(x => new { x.TenantId, x.ClassCode, x.ItemCode, x.Year }).IsUnique();
    }
}
