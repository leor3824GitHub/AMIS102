using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace AMIS.Modules.Vehicle.Data;

internal sealed class VehicleDbInitializer(
    ILogger<VehicleDbInitializer> logger,
    VehicleDbContext context,
    IMediator mediator) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[{Tenant}] applied database migrations for vehicle module", context.TenantInfo?.Identifier);
        }
    }

    // Per-PropertyNo fleet spec for seeded vehicles. PropertyNos must match the LT-class PPE assets
    // seeded by AssetRegisterDbInitializer.VehicleReceivingItems.
    private sealed record SeedSpec(
        string Make, string Model, int Year, VehicleType Type, int Odometer, string Notes,
        string MotorNumber, string ChassisNumber, int Cylinders, int DisplacementCC, string FuelType, string VehicleUse);

    private static readonly IReadOnlyDictionary<string, SeedSpec> SeedSpecs =
        new Dictionary<string, SeedSpec>(StringComparer.OrdinalIgnoreCase)
    {
        ["2026-06-LT-0001"] = new("Toyota", "Innova", 2006, VehicleType.MPV, 450000, "Service shuttle unit",
            "4ZZ-000123", "J28S-000456", 4, 2400, "Diesel", "GOV'T-MPV"),
        ["2026-06-LT-0002"] = new("Toyota", "Vios", 2020, VehicleType.Sedan, 82000, "Admin transport",
            "3NR-000789", "KE7S-000101", 4, 1500, "Gasoline", "GOV'T-UV"),
        ["2026-06-LT-0003"] = new("Isuzu", "D-Max", 2022, VehicleType.PickUp, 65000, "Logistics support",
            "4JJ-000234", "RISEV-000567", 4, 2500, "Diesel", "GOV'T-PU"),
        ["2026-06-LT-0004"] = new("Mitsubishi", "Montero Sport", 2021, VehicleType.SUV, 91000, "Operations field unit",
            "4M41-000345", "MMCE-000678", 6, 3200, "Diesel", "GOV'T-SUV"),
        ["2026-06-LT-0005"] = new("Hyundai", "H100", 2019, VehicleType.Van, 132000, "Delivery van",
            "D4CB-000456", "KMHLN-000789", 4, 2300, "Diesel", "GOV'T-VAN"),
        ["2026-06-LT-0006"] = new("Honda", "City", 2023, VehicleType.Sedan, 28000, "Marketing travel unit",
            "L15B-000567", "MHRVJ-000890", 4, 1500, "Gasoline", "GOV'T-UV"),
    };

    // Plate numbers keyed by PropertyNo (kept separate from technical spec for readability).
    private static readonly IReadOnlyDictionary<string, string> SeedPlates =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["2026-06-LT-0001"] = "SHP-203",
        ["2026-06-LT-0002"] = "ADM-101",
        ["2026-06-LT-0003"] = "LOG-410",
        ["2026-06-LT-0004"] = "OPS-515",
        ["2026-06-LT-0005"] = "DEL-777",
        ["2026-06-LT-0006"] = "MKT-090",
    };

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Vehicles.IgnoreQueryFilters().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("[{Tenant}] vehicle module seed skipped (vehicles already present)", context.TenantInfo?.Identifier);
            return;
        }

        var tenantId = context.TenantInfo?.Identifier ?? MultitenancyConstants.Root.Id;

        // Dogfood the enrollment path: discover unenrolled LT-class PPE assets, then enroll each.
        var enrollable = await mediator.Send(new GetEnrollableVehicleAssetsQuery(), cancellationToken).ConfigureAwait(false);
        if (enrollable.Count == 0)
        {
            logger.LogWarning("[{Tenant}] no enrollable vehicle PPE assets found; skipping vehicle seed (asset seed may not have run yet)", tenantId);
            return;
        }

        var toAdd = new List<VehicleEntity>();
        foreach (var asset in enrollable)
        {
            if (!SeedSpecs.TryGetValue(asset.PropertyNo, out var spec) ||
                !SeedPlates.TryGetValue(asset.PropertyNo, out var plate))
                continue;

            var vehicle = VehicleEntity.Enroll(
                tenantId, asset.AssetRegistryId, asset.PropertyNo,
                asset.UnitCost, asset.AcquisitionDate,
                plate, spec.Make, spec.Model, spec.Year, spec.Type, spec.Odometer, spec.Notes,
                spec.MotorNumber, spec.ChassisNumber, spec.Cylinders, spec.DisplacementCC, spec.FuelType, spec.VehicleUse);
            vehicle.SetCreatedBy("seed");
            toAdd.Add(vehicle);
        }

        if (toAdd.Count == 0)
        {
            logger.LogWarning("[{Tenant}] enrollable vehicle assets did not match any seed spec; no vehicles enrolled", tenantId);
            return;
        }

        await context.Vehicles.AddRangeAsync(toAdd, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[{Tenant}] vehicle module seed enrolled {Count} vehicle(s)", tenantId, toAdd.Count);
    }
}
