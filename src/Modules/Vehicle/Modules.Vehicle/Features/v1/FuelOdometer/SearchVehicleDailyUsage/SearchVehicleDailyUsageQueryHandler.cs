using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Features.v1.FuelOdometer;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer.SearchVehicleDailyUsage;

public sealed class SearchVehicleDailyUsageQueryHandler(VehicleDbContext db)
    : IQueryHandler<SearchVehicleDailyUsageQuery, PagedResponse<VehicleDailyUsageDto>>
{
    public async ValueTask<PagedResponse<VehicleDailyUsageDto>> Handle(SearchVehicleDailyUsageQuery query, CancellationToken ct)
    {
        var q = db.VehicleDailyUsages.AsNoTracking();

        if (query.VehicleId.HasValue)
            q = q.Where(x => x.VehicleId == query.VehicleId.Value);

        if (query.DateFrom.HasValue)
            q = q.Where(x => x.Date >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(x => x.Date <= query.DateTo.Value);

        q = q.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedOnUtc);

        return await q.Select(x => x.ToDto()).ToPagedResponseAsync(query, ct).ConfigureAwait(false);
    }
}
