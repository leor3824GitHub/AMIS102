using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Vehicle.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehicleEntity = FSH.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace FSH.Modules.Vehicle.Data;

internal sealed class VehicleDbInitializer(
    ILogger<VehicleDbInitializer> logger,
    VehicleDbContext context) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for vehicle module", context.TenantInfo?.Identifier);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await context.Vehicles.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

            var vehicles = new[]
            {
                VehicleEntity.Create(tenantId, "SHP-203", "Toyota", "Innova", 2006, VehicleType.MPV, 450000, "Service shuttle unit"),
                VehicleEntity.Create(tenantId, "ADM-101", "Toyota", "Vios", 2020, VehicleType.Sedan, 82000, "Admin transport"),
                VehicleEntity.Create(tenantId, "LOG-410", "Isuzu", "D-Max", 2022, VehicleType.PickUp, 65000, "Logistics support"),
                VehicleEntity.Create(tenantId, "OPS-515", "Mitsubishi", "Montero Sport", 2021, VehicleType.SUV, 91000, "Operations field unit"),
                VehicleEntity.Create(tenantId, "DEL-777", "Hyundai", "H100", 2019, VehicleType.Van, 132000, "Delivery van"),
                VehicleEntity.Create(tenantId, "MKT-090", "Honda", "City", 2023, VehicleType.Sedan, 28000, "Marketing travel unit")
            };

            foreach (var vehicle in vehicles)
            {
                vehicle.CreatedBy = "seed";
            }

            await context.Vehicles.AddRangeAsync(vehicles, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] vehicle module seed completed", context.TenantInfo?.Identifier);
    }
}
