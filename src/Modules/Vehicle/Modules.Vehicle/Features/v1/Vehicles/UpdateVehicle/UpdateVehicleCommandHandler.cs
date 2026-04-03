using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Vehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateVehicle;

public sealed class UpdateVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateVehicleCommand, VehicleDto>
{
    public async ValueTask<VehicleDto> Handle(UpdateVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        var tenantId = currentUser.GetTenant() ?? string.Empty;
        var plateConflict = await db.Vehicles
            .IgnoreQueryFilters()
            .AnyAsync(v => v.TenantId == tenantId && v.PlateNumber == cmd.PlateNumber.ToUpperInvariant() && v.Id != cmd.Id, ct)
            .ConfigureAwait(false);

        if (plateConflict)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.PlateNumber), "Another vehicle already uses this plate number.")]);

        if (!Enum.TryParse<VehicleType>(cmd.Type, ignoreCase: true, out var vehicleType))
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Type), $"Invalid vehicle type '{cmd.Type}'.")]);

        vehicle.Update(cmd.PlateNumber, cmd.Make, cmd.Model, cmd.Year, vehicleType, cmd.Notes,
            cmd.MotorNumber, cmd.ChassisNumber, cmd.NumberOfCylinders,
            cmd.EngineDisplacementCC, cmd.FuelType, cmd.VehicleUse, cmd.AcquisitionCost);
        vehicle.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return vehicle.ToDto();
    }
}
