using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;

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

        vehicle.LastModifiedBy = currentUser.GetUserId().ToString();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
