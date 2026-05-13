using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.RetireVehicle;

public sealed class RetireVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<RetireVehicleCommand, Unit>
{
    public async ValueTask<Unit> Handle(RetireVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        try
        {
            vehicle.Retire();
        }
        catch (InvalidOperationException ex)
        {
            throw new FluentValidation.ValidationException([new ValidationFailure(nameof(cmd.Id), ex.Message)]);
        }

        vehicle.SetLastModifiedBy(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

