using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.GetMotorVehicleInventory;

public sealed class GetMotorVehicleInventoryQueryHandler(VehicleDbContext db, IMediator mediator)
    : IQueryHandler<GetMotorVehicleInventoryQuery, List<MotorVehicleInventoryItemDto>>
{
    public async ValueTask<List<MotorVehicleInventoryItemDto>> Handle(
        GetMotorVehicleInventoryQuery query, CancellationToken cancellationToken)
    {
        var q = db.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<VehicleStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(v => v.Status == status);
        }

        var vehicles = await q
            .OrderBy(v => v.Make).ThenBy(v => v.Model)
            .ToListAsync(cancellationToken);

        // Source the accountable officer from each vehicle's active PAR (batch — avoids N+1).
        var officers = await mediator.Send(
            new GetAccountableOfficersByAssetIdsQuery(vehicles.ConvertAll(v => v.AssetRegistryId)),
            cancellationToken).ConfigureAwait(false);

        return vehicles.Select(v =>
        {
            officers.TryGetValue(v.AssetRegistryId, out var officer);
            return new MotorVehicleInventoryItemDto(
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
                AccountableOfficer: officer?.Name,
                AccountableOfficerTitle: officer?.Designation);
        }).ToList();
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

