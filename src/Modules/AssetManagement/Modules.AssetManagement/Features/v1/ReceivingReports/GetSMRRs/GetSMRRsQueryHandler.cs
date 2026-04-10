using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRs;

public sealed class GetSMRRsQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSMRRsQuery, PagedSMRRsResponse>
{
    public async ValueTask<PagedSMRRsResponse> Handle(GetSMRRsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.SuppliesMaterialsReceivingReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.SMRRNo.ToLower().Contains(kw) ||
                x.ReceivedFrom.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.Date <= query.DateTo.Value);
        }

        if (query.ReceiptType.HasValue)
        {
            q = q.Where(x => x.ReceiptType == query.ReceiptType.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var smrrIds = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.SMRRNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.SMRRItems
            .Where(x => smrrIds.Contains(x.SMRRId))
            .GroupBy(x => x.SMRRId)
            .Select(g => new { SMRRId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SMRRId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var smrrs = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.SMRRNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SMRRSummaryDto(
                x.Id,
                x.SMRRNo,
                x.Date,
                x.ReceivedFrom,
                x.ReceiptType.ToString(),
                x.FundCluster,
                0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = smrrs
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedSMRRsResponse(result, pageNumber, pageSize, totalCount);
    }
}
