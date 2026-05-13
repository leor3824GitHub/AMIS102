using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using AMIS.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.UpdateVehicle;

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

        Enum.TryParse<VehicleType>(cmd.Type, ignoreCase: true, out var vehicleType);

        vehicle.Update(cmd.PlateNumber, cmd.Make, cmd.Model, cmd.Year, vehicleType, cmd.Notes,
            cmd.MotorNumber, cmd.ChassisNumber, cmd.NumberOfCylinders,
            cmd.EngineDisplacementCC, cmd.FuelType, cmd.VehicleUse, cmd.AcquisitionCost);
        vehicle.SetLastModifiedBy(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return vehicle.ToDto();
    }
}

