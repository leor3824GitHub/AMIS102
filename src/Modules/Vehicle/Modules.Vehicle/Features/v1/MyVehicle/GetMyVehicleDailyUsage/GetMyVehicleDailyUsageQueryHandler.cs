using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using AMIS.Modules.Vehicle.Features.v1.MyVehicle;
using AMIS.Framework.Core.Exceptions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.GetMyVehicleDailyUsage;

public sealed class GetMyVehicleDailyUsageQueryHandler(VehicleDbContext db, IMediator mediator)
    : IQueryHandler<GetMyVehicleDailyUsageQuery, VehicleDailyUsageSummaryDto>
{
    public async ValueTask<VehicleDailyUsageSummaryDto> Handle(GetMyVehicleDailyUsageQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == query.VehicleId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Vehicle not found.");

        await MyVehicleGuard.EnsureOwnedAsync(mediator, vehicle.AssetRegistryId, cancellationToken).ConfigureAwait(false);

        var q = db.VehicleDailyUsages.AsNoTracking().Where(x => x.VehicleId == query.VehicleId);
        if (query.DateFrom.HasValue) q = q.Where(x => x.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue) q = q.Where(x => x.Date <= query.DateTo.Value);

        var records = await q
            .Select(x => new { x.Date, x.DistanceKm, x.FuelLiters, x.FuelCost })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalDistance = records.Sum(x => x.DistanceKm);
        var totalLiters = records.Sum(x => x.FuelLiters);
        var totalCost = records.Sum(x => x.FuelCost);
        var dayCount = records.Select(x => x.Date).Distinct().Count();

        return new VehicleDailyUsageSummaryDto(
            records.Count,
            totalDistance,
            totalLiters,
            totalCost,
            VehicleDailyUsage.CalculateKmPerLiter(totalDistance, totalLiters),
            VehicleDailyUsage.CalculateCostPerKm(totalDistance, totalCost),
            dayCount > 0 ? Math.Round((decimal)totalDistance / dayCount, 2) : 0m);
    }
}
