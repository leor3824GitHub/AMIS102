using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.AssetRegister.Domain.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class AssetTransferOfferConfiguration : IEntityTypeConfiguration<AssetTransferOffer>
{
    public void Configure(EntityTypeBuilder<AssetTransferOffer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AssetTransferOffers", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(x => x.FromTenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ToTenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FromAgencyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ToAgencyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SourceIssuanceReportNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ReceivingReportNo).HasMaxLength(64);
        builder.Property(x => x.RejectedReason).HasMaxLength(1000);

        builder.Ignore(x => x.TotalUnitCost);
        builder.Ignore(x => x.TotalNetBookValue);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).AutoInclude(false);

        // The idempotency backstop that lets the offer row act as its own outbox: a re-run of the
        // projector tries to insert the same (tenant, correlation) pair and is rejected, so delivery is
        // at-least-once without an inbox store. Scoped per tenant because both the sender's outbound row
        // and the receiver's inbound row share the correlation id — they just live in different tenants.
        builder.HasIndex(x => new { x.TenantId, x.CorrelationId }).IsUnique();

        // Drives the projector's scan for undelivered work and the receiver's inbox listing.
        builder.HasIndex(x => new { x.TenantId, x.Direction, x.Status });

        // Named, not anonymous: .IsMultiTenant() registers a named tenant filter and EF Core 10 refuses
        // to mix the two forms on one entity (it throws at model build, taking down the whole module).
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}

internal sealed class AssetTransferOfferLineConfiguration : IEntityTypeConfiguration<AssetTransferOfferLine>
{
    public void Configure(EntityTypeBuilder<AssetTransferOfferLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AssetTransferOfferLines", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourcePropertyNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SerialNo).HasMaxLength(200);
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Model).HasMaxLength(200);
        builder.Property(x => x.CatalogUacsCode).HasMaxLength(64);
        builder.Property(x => x.UnitCost).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.NetBookValue).HasPrecision(18, 2);

        builder.HasIndex(x => x.OfferId);
    }
}
