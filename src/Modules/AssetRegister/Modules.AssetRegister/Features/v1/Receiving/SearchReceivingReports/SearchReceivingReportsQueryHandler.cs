using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving.SearchReceivingReports;

public sealed class SearchReceivingReportsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchReceivingReportsQuery, PagedResponse<ReceivingReportSummaryDto>>
{
    public async ValueTask<PagedResponse<ReceivingReportSummaryDto>> Handle(
        SearchReceivingReportsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.ReceivingReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(r => r.ReportNo.ToLower().Contains(k) || r.ReceivedFrom.ToLower().Contains(k));
        }
        if (query.DocumentKind.HasValue) q = q.Where(r => r.DocumentKind == query.DocumentKind.Value);
        if (query.ReceiptType.HasValue) q = q.Where(r => r.ReceiptType == query.ReceiptType.Value);
        if (query.FromDate.HasValue) q = q.Where(r => r.Date >= query.FromDate.Value);
        if (query.ToDate.HasValue) q = q.Where(r => r.Date <= query.ToDate.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderByDescending(r => r.Date).ThenByDescending(r => r.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new ReceivingReportSummaryDto(
                r.Id, r.DocumentKind, r.ReportNo, r.Date, r.ReceivedFrom, r.ReceiptType,
                r.Items.Count,
                r.Items.Sum(i => i.Quantity * i.UnitCost)))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<ReceivingReportSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

