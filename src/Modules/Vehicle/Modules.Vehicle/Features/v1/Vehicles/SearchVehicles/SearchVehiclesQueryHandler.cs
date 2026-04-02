using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Vehicles;
using FSH.Modules.Vehicle.Features.v1.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.SearchVehicles;

public sealed class SearchVehiclesQueryHandler(VehicleDbContext db)
    : IQueryHandler<SearchVehiclesQuery, PagedResponse<VehicleDto>>
{
    public async ValueTask<PagedResponse<VehicleDto>> Handle(SearchVehiclesQuery query, CancellationToken ct)
    {
        var q = db.Vehicles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword}%";
            q = q.Where(v =>
                EF.Functions.ILike(v.PlateNumber, pattern) ||
                EF.Functions.ILike(v.Make, pattern) ||
                EF.Functions.ILike(v.Model, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VehicleStatus>(query.Status, true, out var status))
            q = q.Where(v => v.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<VehicleType>(query.Type, true, out var type))
            q = q.Where(v => v.Type == type);

        if (query.AssignedDepartmentId.HasValue)
            q = q.Where(v => v.AssignedDepartmentId == query.AssignedDepartmentId);

        q = q.OrderBy(v => v.PlateNumber);

        return await q.Select(v => v.ToDto()).ToPagedResponseAsync(query, ct).ConfigureAwait(false);
    }
}
