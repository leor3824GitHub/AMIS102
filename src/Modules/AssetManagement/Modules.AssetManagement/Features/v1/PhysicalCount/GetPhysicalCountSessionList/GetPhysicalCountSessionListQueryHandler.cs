using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionList;

public sealed class GetPhysicalCountSessionListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPhysicalCountSessionListQuery, PagedPhysicalCountSessionResponse>
{
    public async ValueTask<PagedPhysicalCountSessionResponse> Handle(
        GetPhysicalCountSessionListQuery query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.PhysicalCountSessions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.SessionNo.ToLower().Contains(kw) || x.StationOffice.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue) q = q.Where(x => x.CountDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue)   q = q.Where(x => x.CountDate <= query.DateTo.Value);
        if (query.Status.HasValue)   q = q.Where(x => x.Status == query.Status.Value);
        if (query.Scope.HasValue)    q = q.Where(x => x.Scope == query.Scope.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var sessions = await q
            .OrderByDescending(x => x.CountDate).ThenBy(x => x.SessionNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.SessionNo, x.CountDate, x.StationOffice, x.Scope, x.Status, x.CreatedOnUtc })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sessionIds = sessions.Select(x => x.Id).ToList();

        // Aggregate entry counts per session in one query
        var entryCounts = await dbContext.PhysicalCountEntries
            .Where(x => sessionIds.Contains(x.SessionId))
            .GroupBy(x => x.SessionId)
            .Select(g => new
            {
                SessionId      = g.Key,
                Total          = g.Count(),
                Found          = g.Count(e => e.Result == PhysicalCountEntryResult.Found),
                NotFound       = g.Count(e => e.Result == PhysicalCountEntryResult.NotFound),
                FoundAtStation = g.Count(e => e.Result == PhysicalCountEntryResult.FoundAtStation),
                Pending        = g.Count(e => e.Result == null),
            })
            .ToDictionaryAsync(x => x.SessionId, cancellationToken)
            .ConfigureAwait(false);

        var items = sessions.Select(s =>
        {
            entryCounts.TryGetValue(s.Id, out var counts);
            return new PhysicalCountSessionSummaryDto(
                s.Id, s.SessionNo, s.CountDate, s.StationOffice,
                s.Scope.ToString(), s.Status.ToString(),
                counts?.Total          ?? 0,
                counts?.Found          ?? 0,
                counts?.NotFound       ?? 0,
                counts?.FoundAtStation ?? 0,
                counts?.Pending        ?? 0,
                s.CreatedOnUtc);
        }).ToList();

        return new PagedPhysicalCountSessionResponse(items, pageNumber, pageSize, totalCount);
    }
}

