using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Vehicles;
using FSH.Modules.Vehicle.Features.v1.FuelOdometer;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer.UpdateVehicleDailyUsage;

public sealed class UpdateVehicleDailyUsageCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdateVehicleDailyUsageCommand, VehicleDailyUsageDto>
{
    public async ValueTask<VehicleDailyUsageDto> Handle(UpdateVehicleDailyUsageCommand cmd, CancellationToken ct)
    {
        var usage = await db.VehicleDailyUsages
            .FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            .ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Daily usage record not found.")]);

        var vehicle = await db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == usage.VehicleId, ct)
            .ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found.")]);

        if (vehicle.Status == VehicleStatus.Decommissioned)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Cannot update daily usage for a decommissioned vehicle.")]);

        var duplicate = await db.VehicleDailyUsages
            .AsNoTracking()
            .AnyAsync(x => x.VehicleId == usage.VehicleId && x.Date == cmd.Date && x.Id != cmd.Id, ct)
            .ConfigureAwait(false);

        if (duplicate)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Date), "A daily usage record for this vehicle and date already exists.")]);

        usage.Update(
            cmd.Date,
            cmd.OdometerStart,
            cmd.OdometerEnd,
            cmd.FuelLiters,
            cmd.FuelCost,
            cmd.Destination,
            cmd.Remarks);

        usage.LastModifiedBy = currentUser.GetUserId().ToString();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return usage.ToDto();
    }
}
