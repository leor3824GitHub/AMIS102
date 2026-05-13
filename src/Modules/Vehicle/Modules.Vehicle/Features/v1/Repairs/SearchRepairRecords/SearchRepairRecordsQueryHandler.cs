using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using AMIS.Modules.Vehicle.Features.v1.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.SearchRepairRecords;

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

