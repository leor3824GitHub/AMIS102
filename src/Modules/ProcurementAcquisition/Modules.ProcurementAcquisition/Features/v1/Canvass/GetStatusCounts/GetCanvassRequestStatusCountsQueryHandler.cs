using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetStatusCounts;

public sealed class GetCanvassRequestStatusCountsQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetCanvassRequestStatusCountsQuery, IReadOnlyList<CanvassRequestStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<CanvassRequestStatusCountDto>> Handle(
        GetCanvassRequestStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.CanvassRequests.AsNoTracking();

        // Date filter mirrors SearchCanvassRequestsQueryHandler (created-date range, inclusive).
        if (query.FromDate.HasValue)
        {
            var from = new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.CreatedOnUtc >= from);
        }

        if (query.ToDate.HasValue)
        {
            var toExclusive = new DateTimeOffset(query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.CreatedOnUtc < toExclusive);
        }

        var counts = await q
            .GroupBy(x => x.Status)
            .Select(g => new CanvassRequestStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return counts;
    }
}
