using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.ProcurementAcquisition.Domain.Canvass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.ProcurementAcquisition.Data.Configurations;

public sealed class CanvassRequestConfiguration : IEntityTypeConfiguration<CanvassRequest>
{
    public void Configure(EntityTypeBuilder<CanvassRequest> builder)
    {
        builder.ToTable("CanvassRequests", ProcurementAcquisitionModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RivNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        // Version column kept for future xmin-based concurrency; not active until properly wired

        builder.HasIndex(x => new { x.TenantId, x.RivNumber }).IsUnique();
        builder.HasIndex(x => x.PurchaseRequestId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);

        builder.HasMany(x => x.Quotations)
            .WithOne()
            .HasForeignKey(q => q.CanvassRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
