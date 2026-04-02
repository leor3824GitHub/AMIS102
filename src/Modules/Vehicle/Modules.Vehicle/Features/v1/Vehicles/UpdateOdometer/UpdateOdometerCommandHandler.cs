using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateOdometer;

public sealed class UpdateOdometerCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateOdometerCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateOdometerCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        if (cmd.Reading < vehicle.Odometer)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Reading), "Odometer reading cannot be less than the current reading.")]);

        vehicle.UpdateOdometer(cmd.Reading);
        vehicle.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
