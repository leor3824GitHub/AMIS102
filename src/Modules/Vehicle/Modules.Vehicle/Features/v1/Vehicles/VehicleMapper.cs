using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using VehicleEntity = AMIS.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles;

internal static class VehicleMapper
{
    internal static VehicleDto ToDto(this VehicleEntity v) =>
        new(v.Id, v.AssetRegistryId, v.PropertyNo, v.PlateNumber, v.Make, v.Model, v.Year,
            v.Type.ToString(), v.Status.ToString(), v.Odometer,
            v.Notes,
            v.MotorNumber, v.ChassisNumber,
            v.NumberOfCylinders, v.EngineDisplacementCC,
            v.FuelType, v.VehicleUse, v.AcquisitionCost, v.AcquisitionDate,
            v.CreatedOnUtc, v.CreatedBy,
            v.LastModifiedOnUtc, v.LastModifiedBy);
}

