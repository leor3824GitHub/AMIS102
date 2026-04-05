using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Domain.FuelOdometer;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer;

internal static class VehicleDailyUsageMapper
{
    public static VehicleDailyUsageDto ToDto(this VehicleDailyUsage usage)
    {
        var kmPerLiter = usage.FuelLiters > 0 ? Math.Round(usage.DistanceKm / usage.FuelLiters, 4) : 0m;
        var costPerKm = usage.DistanceKm > 0 ? Math.Round(usage.FuelCost / usage.DistanceKm, 4) : 0m;

        return new VehicleDailyUsageDto(
            usage.Id,
            usage.VehicleId,
            usage.Date,
            usage.OdometerStart,
            usage.OdometerEnd,
            usage.DistanceKm,
            usage.FuelLiters,
            usage.FuelCost,
            kmPerLiter,
            costPerKm,
            usage.Destination,
            usage.Remarks,
            usage.CreatedOnUtc,
            usage.CreatedBy,
            usage.LastModifiedOnUtc,
            usage.LastModifiedBy);
    }
}
