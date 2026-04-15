using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.GetSMIRList;

public sealed class GetSMIRListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSMIRListQuery, PagedSMIRListResponse>
{
    public async ValueTask<PagedSMIRListResponse> Handle(GetSMIRListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var q = dbContext.SemiExpendableIssuanceRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where(x => x.SMIRNo.Contains(query.Keyword));
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.Date <= query.DateTo.Value);
        }

        if (query.IssuanceType.HasValue)
        {
            q = q.Where(x => x.IssuanceType == query.IssuanceType.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TransferredToTenantId))
        {
            q = q.Where(x => x.TransferredToTenantId == query.TransferredToTenantId);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var smirIds = await q
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.SMIRItems
            .Where(x => smirIds.Contains(x.SMIRId))
            .GroupBy(x => x.SMIRId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = await dbContext.SemiExpendableIssuanceRecords
            .Where(x => smirIds.Contains(x.Id))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Select(x => new SMIRSummaryDto(
                x.Id,
                x.SMIRNo,
                x.Date,
                x.IssuanceType.ToString(),
                x.TransferredToTenantId,
                x.TransferredToOfficerName,
                x.IssuedByEmployeeId,
                0,
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = items
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedSMIRListResponse(result, pageNumber, pageSize, totalCount);
    }
}
