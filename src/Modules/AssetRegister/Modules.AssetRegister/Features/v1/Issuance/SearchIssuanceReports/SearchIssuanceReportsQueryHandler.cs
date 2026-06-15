using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.SearchIssuanceReports;

public sealed class SearchIssuanceReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchIssuanceReportsQuery, PagedResponse<PropertyIssuanceReportSummaryDto>>
{
    public async ValueTask<PagedResponse<PropertyIssuanceReportSummaryDto>> Handle(
        SearchIssuanceReportsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = db.PropertyIssuanceReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.ReportNo.ToLower().Contains(k));
        }
        if (query.ReportType.HasValue) q = q.Where(r => r.ReportType == query.ReportType.Value);
        if (query.Nature.HasValue) q = q.Where(r => r.Nature == query.Nature.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(r => r.Date <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new PropertyIssuanceReportSummaryDto(
                r.Id, r.ReportNo, r.ReportType, r.Nature, r.Date,
                r.Lines.Count, r.Lines.Sum(l => l.SnapshotAmount)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

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
