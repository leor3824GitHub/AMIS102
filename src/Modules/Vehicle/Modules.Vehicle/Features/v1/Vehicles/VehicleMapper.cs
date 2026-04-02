using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using VehicleEntity = FSH.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles;

internal static class VehicleMapper
{
    internal static VehicleDto ToDto(this VehicleEntity v) =>
        new(v.Id, v.PlateNumber, v.Make, v.Model, v.Year,
            v.Type.ToString(), v.Status.ToString(), v.Odometer,
            v.AssignedDepartmentId, v.AssignedDepartment,
            v.AssignedDriverId, v.AssignedDriver,
            v.Notes,
            v.CreatedOnUtc, v.CreatedBy,
            v.LastModifiedOnUtc, v.LastModifiedBy);
}
