using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.SearchIncidentReports;

public sealed class SearchIncidentReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchIncidentReportsQuery, PagedResponse<PropertyIncidentReportSummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyIncidentReportSummaryDto>> Handle(
        SearchIncidentReportsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.PropertyIncidentReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.IncidentNo.ToLower().Contains(k) || r.Circumstances.ToLower().Contains(k));
        }
        if (query.IncidentType.HasValue) q = q.Where(r => r.IncidentType == query.IncidentType.Value);
        if (query.Status.HasValue) q = q.Where(r => r.Status == query.Status.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.IncidentDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(r => r.IncidentDate <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.IncidentDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new PropertyIncidentReportSummaryDto(
                r.Id, r.IncidentNo, r.IncidentType, r.Status, r.IncidentDate, r.Items.Count))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<PropertyIncidentReportSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

