using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;

public sealed class AssignVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<AssignVehicleCommand, Unit>
{
    public async ValueTask<Unit> Handle(AssignVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        vehicle.AssignTo(cmd.DepartmentId, cmd.DepartmentName, cmd.DriverId, cmd.DriverName, cmd.AccountableOfficerTitle);
        vehicle.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
