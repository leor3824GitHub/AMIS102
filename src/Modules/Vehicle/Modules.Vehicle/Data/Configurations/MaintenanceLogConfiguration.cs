using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.Vehicle.Data.Configurations;

public class MaintenanceLogConfiguration : IEntityTypeConfiguration<MaintenanceLog>
{
    public void Configure(EntityTypeBuilder<MaintenanceLog> builder)
    {
        builder.ToTable("MaintenanceLogs", VehicleModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(l => l.MaintenanceType).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Property(l => l.Cost).HasPrecision(18, 2);
        builder.Property(l => l.PerformedBy).HasMaxLength(255);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.Version).IsConcurrencyToken();
        builder.Property(l => l.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(l => new { l.TenantId, l.VehicleId });
        builder.HasIndex(l => new { l.TenantId, l.PerformedDate });

        builder.HasQueryFilter("SoftDelete", l => !l.IsDeleted);
    }
}

