using AMIS.Modules.ProcurementAcquisition.Domain.JobOrders;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class JoNumberSequenceConfiguration : IEntityTypeConfiguration<JoNumberSequence>
{
    public void Configure(EntityTypeBuilder<JoNumberSequence> builder)
    {
        builder.ToTable("JoNumberSequences", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.LastSerial).IsRequired().HasDefaultValue(0);

        // PostgreSQL xmin is a system column tracking the transaction that last modified the row.
        // It updates automatically on every UPDATE, giving true optimistic concurrency without a separate column.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // One counter row per (TenantId + Year + Month) — JO serials reset monthly.
        builder.HasIndex(x => new { x.TenantId, x.Year, x.Month }).IsUnique();
    }
}
