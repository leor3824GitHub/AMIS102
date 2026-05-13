using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;

public sealed class ReactivateVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<ReactivateVehicleCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReactivateVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        try
        {
            vehicle.Reactivate();
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

