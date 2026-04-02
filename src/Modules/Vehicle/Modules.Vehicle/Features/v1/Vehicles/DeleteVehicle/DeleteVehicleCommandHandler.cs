using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;

public sealed class DeleteVehicleCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<DeleteVehicleCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        vehicle.SoftDelete(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
