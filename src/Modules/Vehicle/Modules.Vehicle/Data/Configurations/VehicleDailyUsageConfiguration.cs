using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace AMIS.Modules.Vehicle.Data.Configurations;

public sealed class VehicleDailyUsageConfiguration : IEntityTypeConfiguration<VehicleDailyUsage>
{
    public void Configure(EntityTypeBuilder<VehicleDailyUsage> builder)
    {
        builder.ToTable("VehicleDailyUsages", VehicleModuleConstants.SchemaName).IsMultiTenant();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Destination).HasMaxLength(250);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.FuelLiters).HasPrecision(18, 3);
        builder.Property(x => x.FuelCost).HasPrecision(18, 2);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasOne<VehicleEntity>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.Date }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Date });

        builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);
    }
}

