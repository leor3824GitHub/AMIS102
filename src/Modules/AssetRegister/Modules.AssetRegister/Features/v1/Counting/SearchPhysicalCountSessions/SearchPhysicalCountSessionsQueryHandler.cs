using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.Counting;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Counting.SearchPhysicalCountSessions;

public sealed class SearchPhysicalCountSessionsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchPhysicalCountSessionsQuery, PagedResponse<PhysicalCountSessionSummaryDto>>
{
    public async ValueTask<PagedResponse<PhysicalCountSessionSummaryDto>> Handle(
        SearchPhysicalCountSessionsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.PhysicalCountSessions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(s => s.Code.ToLower().Contains(k));
        }
        if (query.Status.HasValue) q = q.Where(s => s.Status == query.Status.Value);
        if (query.Scope.HasValue) q = q.Where(s => s.Scope == query.Scope.Value);
        if (query.FromDate.HasValue) q = q.Where(s => s.AsAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(s => s.AsAt <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(s => s.AsAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(s => new PhysicalCountSessionSummaryDto(
                s.Id, s.Code, s.Scope, s.Status, s.AsAt, s.StartedOn, s.ClosedOn, s.Entries.Count))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<PhysicalCountSessionSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
