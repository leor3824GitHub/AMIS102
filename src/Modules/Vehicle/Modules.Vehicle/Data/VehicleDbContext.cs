using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Persistence.Context;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using AMIS.Modules.Vehicle.Domain.Repairs;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Vehicle.Data;

public class VehicleDbContext : BaseDbContext
{
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<RepairRecord> RepairRecords => Set<RepairRecord>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();
    public DbSet<VehicleDailyUsage> VehicleDailyUsages => Set<VehicleDailyUsage>();

    public VehicleDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<VehicleDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleDbContext).Assembly);
    }
}

