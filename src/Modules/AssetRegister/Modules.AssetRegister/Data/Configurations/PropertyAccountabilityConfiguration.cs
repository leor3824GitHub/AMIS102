using AMIS.Modules.AssetRegister.Domain.Accountability;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMIS.Modules.AssetRegister.Data.Configurations;

internal sealed class PropertyAccountabilityConfiguration : IEntityTypeConfiguration<PropertyAccountability>
{
    public void Configure(EntityTypeBuilder<PropertyAccountability> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyAccountabilities", AssetRegisterModuleConstants.SchemaName)
            .IsMultiTenant();

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).IsRowVersion();
        builder.Property(x => x.DocumentNo).IsRequired().HasMaxLength(64);
        builder.Property(x => x.FundCluster).IsRequired().HasMaxLength(64);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);

        builder.OwnsOne(x => x.IssuedBy, n => n.ConfigureEmployeeRef("IssuedBy"));
        builder.OwnsOne(x => x.ReceivedBy, n => n.ConfigureEmployeeRef("ReceivedBy"));
        builder.Navigation(x => x.IssuedBy).IsRequired();
        builder.Navigation(x => x.ReceivedBy).IsRequired();

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.AccountabilityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lines).AutoInclude(false);

        builder.HasIndex(x => new { x.TenantId, x.DocumentNo }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresOn });
    }
}

internal sealed class PropertyAccountabilityLineConfiguration : IEntityTypeConfiguration<PropertyAccountabilityLine>
{
    public void Configure(EntityTypeBuilder<PropertyAccountabilityLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PropertyAccountabilityLines", AssetRegisterModuleConstants.SchemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotItemNo).IsRequired().HasMaxLength(32);
        builder.Property(x => x.SnapshotResponsibilityCenterCode).HasMaxLength(64);

        builder.OwnsOne(x => x.Snapshot, n => n.ConfigureSnapshot());
        builder.Navigation(x => x.Snapshot).IsRequired();

        builder.OwnsOne(x => x.VehicleProfile, v =>
        {
            v.Property(p => p.OdometerAtIssue).HasColumnName("vehicle_odometer_at_issue");
            v.Property(p => p.OdometerAtReturn).HasColumnName("vehicle_odometer_at_return");
            v.Property(p => p.PlateNumber).HasMaxLength(32).HasColumnName("vehicle_plate_number");
            v.Property(p => p.EngineNumber).HasMaxLength(64).HasColumnName("vehicle_engine_number");
            v.Property(p => p.ChassisNumber).HasMaxLength(64).HasColumnName("vehicle_chassis_number");
        });

        builder.HasIndex(x => new { x.AccountabilityId, x.LineStatus });
        builder.HasIndex(x => x.AssetRegistryId);
    }
}

