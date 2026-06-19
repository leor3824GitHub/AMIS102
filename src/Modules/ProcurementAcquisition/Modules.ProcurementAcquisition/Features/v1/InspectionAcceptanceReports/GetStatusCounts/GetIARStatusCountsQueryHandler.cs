using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetStatusCounts;

public sealed class GetIARStatusCountsQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetIARStatusCountsQuery, IReadOnlyList<IARStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<IARStatusCountDto>> Handle(
        GetIARStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.InspectionAcceptanceReports.AsNoTracking();

        // Date filter mirrors SearchInspectionAcceptanceReportsQueryHandler (IAR-date range, inclusive).
        if (query.FromDate.HasValue)
            q = q.Where(x => x.IarDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.IarDate <= query.ToDate.Value);

        var counts = await q
            .GroupBy(x => x.Status)
            .Select(g => new IARStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return counts;
    }
}
