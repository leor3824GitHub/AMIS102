using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace AMIS.Modules.Vehicle.Data;

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
                VehicleEntity.Create(
                    tenantId, "SHP-203", "Toyota", "Innova", 2006, VehicleType.MPV, 450000, "Service shuttle unit",
                    motorNumber: "4ZZ-000123", chassisNumber: "J28S-000456",
                    numberOfCylinders: 4, engineDisplacementCC: 2400,
                    fuelType: "Diesel", vehicleUse: "GOV'T-MPV", acquisitionCost: 950000m),
                    
                VehicleEntity.Create(
                    tenantId, "ADM-101", "Toyota", "Vios", 2020, VehicleType.Sedan, 82000, "Admin transport",
                    motorNumber: "3NR-000789", chassisNumber: "KE7S-000101",
                    numberOfCylinders: 4, engineDisplacementCC: 1500,
                    fuelType: "Gasoline", vehicleUse: "GOV'T-UV", acquisitionCost: 850000m),
                    
                VehicleEntity.Create(
                    tenantId, "LOG-410", "Isuzu", "D-Max", 2022, VehicleType.PickUp, 65000, "Logistics support",
                    motorNumber: "4JJ-000234", chassisNumber: "RISEV-000567",
                    numberOfCylinders: 4, engineDisplacementCC: 2500,
                    fuelType: "Diesel", vehicleUse: "GOV'T-PU", acquisitionCost: 1200000m),
                    
                VehicleEntity.Create(
                    tenantId, "OPS-515", "Mitsubishi", "Montero Sport", 2021, VehicleType.SUV, 91000, "Operations field unit",
                    motorNumber: "4M41-000345", chassisNumber: "MMCE-000678",
                    numberOfCylinders: 6, engineDisplacementCC: 3200,
                    fuelType: "Diesel", vehicleUse: "GOV'T-SUV", acquisitionCost: 1500000m),
                    
                VehicleEntity.Create(
                    tenantId, "DEL-777", "Hyundai", "H100", 2019, VehicleType.Van, 132000, "Delivery van",
                    motorNumber: "D4CB-000456", chassisNumber: "KMHLN-000789",
                    numberOfCylinders: 4, engineDisplacementCC: 2300,
                    fuelType: "Diesel", vehicleUse: "GOV'T-VAN", acquisitionCost: 1100000m),
                    
                VehicleEntity.Create(
                    tenantId, "MKT-090", "Honda", "City", 2023, VehicleType.Sedan, 28000, "Marketing travel unit",
                    motorNumber: "L15B-000567", chassisNumber: "MHRVJ-000890",
                    numberOfCylinders: 4, engineDisplacementCC: 1500,
                    fuelType: "Gasoline", vehicleUse: "GOV'T-UV", acquisitionCost: 900000m)
            };

            foreach (var vehicle in vehicles)
            {
                vehicle.SetCreatedBy("seed");
            }

            await context.Vehicles.AddRangeAsync(vehicles, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[{Tenant}] vehicle module seed completed", context.TenantInfo?.Identifier);
    }
}

