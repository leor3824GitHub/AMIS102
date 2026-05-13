using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using AMIS.Modules.Vehicle.Features.v1.FuelOdometer;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.FuelOdometer.CreateVehicleDailyUsage;

public sealed class CreateVehicleDailyUsageCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreateVehicleDailyUsageCommand, VehicleDailyUsageDto>
{
    public async ValueTask<VehicleDailyUsageDto> Handle(CreateVehicleDailyUsageCommand cmd, CancellationToken ct)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == cmd.VehicleId, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.VehicleId), "Vehicle not found.")]);

        if (vehicle.Status == VehicleStatus.Decommissioned)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.VehicleId), "Cannot record daily usage for a decommissioned vehicle.")]);

        var exists = await db.VehicleDailyUsages
            .AsNoTracking()
            .AnyAsync(x => x.VehicleId == cmd.VehicleId && x.Date == cmd.Date, ct)
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
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return usage.ToDto();
    }
}

