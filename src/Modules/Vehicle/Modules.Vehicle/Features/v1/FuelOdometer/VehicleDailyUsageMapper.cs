using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;

namespace AMIS.Modules.Vehicle.Features.v1.FuelOdometer;

internal static class VehicleDailyUsageMapper
{
    public static VehicleDailyUsageDto ToDto(this VehicleDailyUsage usage)
    {
        return new VehicleDailyUsageDto(
            usage.Id,
            usage.VehicleId,
            usage.Date,
            usage.OdometerStart,
            usage.OdometerEnd,
            usage.DistanceKm,
            usage.FuelLiters,
            usage.FuelCost,
            usage.KmPerLiter,
            usage.CostPerKm,
            usage.Destination,
            usage.Remarks,
            usage.CreatedOnUtc,
            usage.CreatedBy,
            usage.LastModifiedOnUtc,
            usage.LastModifiedBy);
    }
}

