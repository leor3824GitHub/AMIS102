using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer;
using AMIS.Modules.Vehicle.Features.v1.MyVehicle;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.RecordMyVehicleFuelOdometer;

public sealed class RecordMyVehicleFuelOdometerCommandHandler(VehicleDbContext db, IMediator mediator, ICurrentUser currentUser)
    : ICommandHandler<RecordMyVehicleFuelOdometerCommand, VehicleDailyUsageDto>
{
    public async ValueTask<VehicleDailyUsageDto> Handle(RecordMyVehicleFuelOdometerCommand cmd, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.VehicleId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Vehicle not found.");

        await MyVehicleGuard.EnsureOwnedAsync(mediator, vehicle.AssetRegistryId, cancellationToken).ConfigureAwait(false);

        if (vehicle.Status == VehicleStatus.Decommissioned)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.VehicleId), "Cannot record daily usage for a decommissioned vehicle.")]);

        var exists = await db.VehicleDailyUsages
            .AsNoTracking()
            .AnyAsync(x => x.VehicleId == cmd.VehicleId && x.Date == cmd.Date, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Date), "A daily usage record for this vehicle and date already exists.")]);

        var usage = VehicleDailyUsage.Create(
            currentUser.GetTenant() ?? string.Empty,
            cmd.VehicleId,
            cmd.Date,
            cmd.OdometerStart,
            cmd.OdometerEnd,
            cmd.FuelLiters,
            cmd.FuelCost,
            cmd.Destination,
            cmd.Remarks);

        usage.SetCreatedBy(currentUser.GetUserId().ToString());

        db.VehicleDailyUsages.Add(usage);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return usage.ToDto();
    }
}
