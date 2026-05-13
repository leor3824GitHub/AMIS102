using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.FuelOdometer;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.FuelOdometer.GetVehicleDailyUsageSummary;

public sealed class GetVehicleDailyUsageSummaryQueryHandler(VehicleDbContext db)
    : IQueryHandler<GetVehicleDailyUsageSummaryQuery, VehicleDailyUsageSummaryDto>
{
    public async ValueTask<VehicleDailyUsageSummaryDto> Handle(GetVehicleDailyUsageSummaryQuery query, CancellationToken ct)
    {
        var q = db.VehicleDailyUsages.AsNoTracking();

        if (query.VehicleId.HasValue)
            q = q.Where(x => x.VehicleId == query.VehicleId.Value);

        if (query.DateFrom.HasValue)
            q = q.Where(x => x.Date >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(x => x.Date <= query.DateTo.Value);

        var records = await q
            .Select(x => new { x.Date, x.DistanceKm, x.FuelLiters, x.FuelCost })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var recordCount = records.Count;
        var totalDistance = records.Sum(x => x.DistanceKm);
        var totalLiters = records.Sum(x => x.FuelLiters);
        var totalCost = records.Sum(x => x.FuelCost);
        var dayCount = records.Select(x => x.Date).Distinct().Count();

        var avgKmPerLiter = VehicleDailyUsage.CalculateKmPerLiter(totalDistance, totalLiters);
        var avgCostPerKm = VehicleDailyUsage.CalculateCostPerKm(totalDistance, totalCost);
        var avgDistancePerDay = dayCount > 0 ? Math.Round((decimal)totalDistance / dayCount, 2) : 0m;

        return new VehicleDailyUsageSummaryDto(
            recordCount,
            totalDistance,
            totalLiters,
            totalCost,
            avgKmPerLiter,
            avgCostPerKm,
            avgDistancePerDay);
    }
}

