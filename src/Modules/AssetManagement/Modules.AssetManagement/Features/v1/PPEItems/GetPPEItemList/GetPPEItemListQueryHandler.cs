using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEItems.GetPPEItemList;

public sealed class GetPPEItemListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPEItemListQuery, PagedPPEItemResponse>
{
    public async ValueTask<PagedPPEItemResponse> Handle(GetPPEItemListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PPEItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.PropertyCode.ToLower().Contains(kw) ||
                x.PropertyNumber.ToLower().Contains(kw) ||
                x.Description.ToLower().Contains(kw));
        }

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.CurrentAccountableEmployeeId.HasValue)
            q = q.Where(x => x.CurrentAccountableEmployeeId == query.CurrentAccountableEmployeeId.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var rows = await q
            .OrderBy(x => x.PropertyNumber)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new PPEItemSummaryDto(
                x.Id,
                x.PropertyCode,
                x.PropertyNumber,
                x.Description,
                x.SerialNumber,
                x.DateAcquired,
                x.UnitCost,
                x.Status.ToString(),
                x.CurrentAccountableEmployeeId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PagedPPEItemResponse(rows, pageNumber, pageSize, totalCount);
    }
}
