using AMIS.Framework.Persistence.SignedDocuments;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class CanvassRequestConfiguration : IEntityTypeConfiguration<CanvassRequest>
{
    public void Configure(EntityTypeBuilder<CanvassRequest> builder)
    {
        builder.ToTable("CanvassRequests", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.ConfigureSignedCopy(x => x.SignedCopy);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RivNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        // PostgreSQL xmin system column — true optimistic concurrency, auto-updated by the DB on every UPDATE.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.RivNumber }).IsUnique();
        builder.HasIndex(x => x.PurchaseRequestId);
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasMany(x => x.Quotations)
            .WithOne()
            .HasForeignKey(q => q.CanvassRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(x => x.LineItems, b =>
        {
            b.ToJson();
            b.Property(li => li.PrItemNo).IsRequired();
            b.Property(li => li.Description).HasMaxLength(500).IsRequired();
            b.Property(li => li.Unit).HasMaxLength(64).IsRequired();
            b.Property(li => li.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.EstimatedUnitCost).HasPrecision(18, 4).IsRequired();
            b.Property(li => li.UacsObjectCode).HasMaxLength(64);
            // Per-line split-award winner (null until awarded).
            b.Property(li => li.AwardedUnitPrice).HasPrecision(18, 4);
        });

        // ROPC committee frozen at award time (Abstract of Canvass faithful reprint).
        builder.OwnsMany(x => x.AwardSignatories, b =>
        {
            b.ToJson();
            b.Property(s => s.SortOrder).IsRequired();
            b.Property(s => s.Name).HasMaxLength(200);
            b.Property(s => s.Role).HasMaxLength(200);
        });
    }
}

