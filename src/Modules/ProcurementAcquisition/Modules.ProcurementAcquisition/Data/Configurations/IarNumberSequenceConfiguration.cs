using AMIS.Modules.ProcurementAcquisition.Domain.AssetInspectionAcceptanceReports;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class IarNumberSequenceConfiguration : IEntityTypeConfiguration<IarNumberSequence>
{
    public void Configure(EntityTypeBuilder<IarNumberSequence> builder)
    {
        builder.ToTable("IarNumberSequences", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.LastSerial).IsRequired().HasDefaultValue(0);

        // PostgreSQL xmin is a system column tracking the transaction that last modified the row.
        // It updates automatically on every UPDATE, giving true optimistic concurrency without a separate column.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // One counter row per (TenantId + Year)
        builder.HasIndex(x => new { x.TenantId, x.Year }).IsUnique();
    }
}
