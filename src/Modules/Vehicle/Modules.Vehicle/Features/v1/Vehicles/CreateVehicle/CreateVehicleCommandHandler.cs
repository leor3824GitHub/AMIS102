using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Vehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;
using VehicleEntity = FSH.Modules.Vehicle.Domain.Vehicles.Vehicle;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;

public sealed class CreateVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateVehicleCommand, VehicleDto>
{
    public async ValueTask<VehicleDto> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var plateExists = await db.Vehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && v.PlateNumber == cmd.PlateNumber.ToUpperInvariant(), ct)
            .ConfigureAwait(false);

        if (plateExists)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.PlateNumber), "A vehicle with this plate number already exists.")]);

        Enum.TryParse<VehicleType>(cmd.Type, ignoreCase: true, out var vehicleType);

        var vehicle = VehicleEntity.Create(tenantId, cmd.PlateNumber, cmd.Make, cmd.Model,
            cmd.Year, vehicleType, cmd.Odometer, cmd.Notes,
            cmd.MotorNumber, cmd.ChassisNumber, cmd.NumberOfCylinders,
            cmd.EngineDisplacementCC, cmd.FuelType, cmd.VehicleUse, cmd.AcquisitionCost);
        vehicle.SetCreatedBy(currentUser.GetUserId().ToString());

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return vehicle.ToDto();
    }
}
