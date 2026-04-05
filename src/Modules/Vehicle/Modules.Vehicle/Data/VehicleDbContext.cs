using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Domain.FuelOdometer;
using FSH.Modules.Vehicle.Domain.Maintenance;
using FSH.Modules.Vehicle.Domain.Repairs;
using VehicleEntity = FSH.Modules.Vehicle.Domain.Vehicles.Vehicle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Vehicle.Data;

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
