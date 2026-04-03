using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.GetMotorVehicleInventory;

public sealed class GetMotorVehicleInventoryQueryHandler(VehicleDbContext db)
    : IQueryHandler<GetMotorVehicleInventoryQuery, List<MotorVehicleInventoryItemDto>>
{
    public async ValueTask<List<MotorVehicleInventoryItemDto>> Handle(
        GetMotorVehicleInventoryQuery query, CancellationToken ct)
    {
        var q = db.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<VehicleStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(v => v.Status == status);
        }

        var vehicles = await q
            .OrderBy(v => v.Make).ThenBy(v => v.Model)
            .ToListAsync(ct);

        return vehicles.Select(v => new MotorVehicleInventoryItemDto(
            Qty: 1,
            Description: $"{v.Make} {v.Model}",
            MotorNumber: v.MotorNumber,
            ChassisNumber: v.ChassisNumber,
            VehicleClassification: BuildClassification(v),
            PlateNumber: v.PlateNumber,
            VehicleUse: v.VehicleUse,
            NumberOfCylinders: v.NumberOfCylinders,
            EngineDisplacementCC: v.EngineDisplacementCC,
            FuelType: v.FuelType,
            Year: v.Year,
            AcquisitionCost: v.AcquisitionCost,
            RunningCondition: BuildRunningCondition(v.Status),
            AccountableOfficer: v.AssignedDriver,
            AccountableOfficerTitle: v.AccountableOfficerTitle
        )).ToList();
    }

    private static string BuildClassification(Domain.Vehicles.Vehicle v)
    {
        // Use VehicleUse if set, otherwise derive from Type
        return v.Type switch
        {
            VehicleType.PickUp  => "PICK-UP VEHICLE",
            VehicleType.Van     => "VAN TYPE VEHICLE",
            VehicleType.SUV     => "SUV TYPE VEHICLE",
            VehicleType.Truck   => "TRUCK TYPE VEHICLE",
            VehicleType.Sedan   => "SEDAN TYPE VEHICLE",
            VehicleType.Bus     => "BUS TYPE VEHICLE",
            VehicleType.Motorcycle => "MOTORCYCLE",
            VehicleType.MPV     => "MPV TYPE VEHICLE",
            _                   => $"{v.Type} TYPE VEHICLE"
        };
    }

    private static string BuildRunningCondition(VehicleStatus status) => status switch
    {
        VehicleStatus.Active       => "IN GOOD RUNNING CONDITION",
        VehicleStatus.UnderRepair  => "UNDER REPAIR",
        VehicleStatus.Retired      => "RETIRED",
        VehicleStatus.Decommissioned => "DECOMMISSIONED",
        _                          => status.ToString().ToUpperInvariant()
    };
}
