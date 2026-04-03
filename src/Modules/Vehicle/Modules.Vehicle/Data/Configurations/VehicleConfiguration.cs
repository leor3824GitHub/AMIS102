using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleEntity = FSH.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace FSH.Modules.Vehicle.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<VehicleEntity>
{
    public void Configure(EntityTypeBuilder<VehicleEntity> builder)
    {
        builder.ToTable("Vehicles", VehicleModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(v => v.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Make).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Type).HasConversion<int>();
        builder.Property(v => v.Status).HasConversion<int>();
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.AssignedDepartment).HasMaxLength(255);
        builder.Property(v => v.AssignedDriver).HasMaxLength(255);
        builder.Property(v => v.AccountableOfficerTitle).HasMaxLength(255);

        // Technical specification fields
        builder.Property(v => v.MotorNumber).HasMaxLength(100);
        builder.Property(v => v.ChassisNumber).HasMaxLength(100);
        builder.Property(v => v.FuelType).HasMaxLength(50);
        builder.Property(v => v.VehicleUse).HasMaxLength(100);
        builder.Property(v => v.AcquisitionCost).HasPrecision(18, 2);

        builder.Property(v => v.Version).IsConcurrencyToken();
        builder.Property(v => v.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(v => new { v.TenantId, v.PlateNumber }).IsUnique();
        builder.HasIndex(v => new { v.TenantId, v.Status });

        builder.HasQueryFilter("SoftDelete", v => !v.IsDeleted);
    }
}
