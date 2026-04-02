using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Repairs;
using FSH.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;

public sealed class SearchRepairRecordsQueryHandler(VehicleDbContext db)
    : IQueryHandler<SearchRepairRecordsQuery, PagedResponse<RepairRecordDto>>
{
    public async ValueTask<PagedResponse<RepairRecordDto>> Handle(SearchRepairRecordsQuery query, CancellationToken ct)
    {
        var q = db.RepairRecords.AsNoTracking();

        if (query.VehicleId.HasValue)
            q = q.Where(r => r.VehicleId == query.VehicleId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<RepairStatus>(query.Status, true, out var status))
            q = q.Where(r => r.Status == status);

        if (query.DateFrom.HasValue)
            q = q.Where(r => r.RepairDate >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(r => r.RepairDate <= query.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword}%";
            q = q.Where(r =>
                EF.Functions.ILike(r.Description, pattern) ||
                (r.VendorName != null && EF.Functions.ILike(r.VendorName, pattern)));
        }

        q = q.OrderByDescending(r => r.RepairDate);

        return await q.Select(r => r.ToDto()).ToPagedResponseAsync(query, ct).ConfigureAwait(false);
    }
}
