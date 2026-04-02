using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Vehicle.Data.Configurations;

public class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<MaintenanceSchedule> builder)
    {
        builder.ToTable("MaintenanceSchedules", VehicleModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(s => s.MaintenanceType).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.Version).IsConcurrencyToken();
        builder.Property(s => s.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(s => new { s.TenantId, s.VehicleId });
        builder.HasIndex(s => new { s.TenantId, s.IsActive });

        builder.HasQueryFilter("SoftDelete", s => !s.IsDeleted);
    }
}
