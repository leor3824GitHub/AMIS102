using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Vehicle.Domain.Repairs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Vehicle.Data.Configurations;

public class RepairRecordConfiguration : IEntityTypeConfiguration<RepairRecord>
{
    public void Configure(EntityTypeBuilder<RepairRecord> builder)
    {
        builder.ToTable("RepairRecords", VehicleModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Cost).HasPrecision(18, 2);
        builder.Property(r => r.VendorName).HasMaxLength(255);
        builder.Property(r => r.VendorContact).HasMaxLength(500);
        builder.Property(r => r.PartsUsed).HasMaxLength(2000);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.Version).IsConcurrencyToken();
        builder.Property(r => r.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(r => new { r.TenantId, r.VehicleId });
        builder.HasIndex(r => new { r.TenantId, r.Status });

        builder.HasQueryFilter("SoftDelete", r => !r.IsDeleted);
    }
}
