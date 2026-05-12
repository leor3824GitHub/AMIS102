using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.SearchIssuanceReports;

public sealed class SearchIssuanceReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchIssuanceReportsQuery, PagedResponse<PropertyIssuanceReportSummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyIssuanceReportSummaryDto>> Handle(
        SearchIssuanceReportsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.PropertyIssuanceReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.ReportNo.ToLower().Contains(k));
        }
        if (query.ReportType.HasValue) q = q.Where(r => r.ReportType == query.ReportType.Value);
        if (query.Status.HasValue) q = q.Where(r => r.Status == query.Status.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.PeriodFromDate >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(r => r.PeriodToDate <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.PeriodFromDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new PropertyIssuanceReportSummaryDto(
                r.Id, r.ReportNo, r.ReportType, r.Status, r.PeriodFromDate, r.PeriodToDate,
                r.Lines.Count, r.Lines.Sum(l => l.SnapshotAmount)))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<PropertyIssuanceReportSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}
