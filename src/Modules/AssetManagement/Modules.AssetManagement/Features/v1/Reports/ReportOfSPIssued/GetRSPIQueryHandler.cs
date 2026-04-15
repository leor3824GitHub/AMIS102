using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;

public sealed class GetRSPIQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRSPIQuery, PagedRSPIResponse>
{
    public async ValueTask<PagedRSPIResponse> Handle(GetRSPIQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        var q =
            from icsItem in dbContext.ICSItems
            join prop in dbContext.SemiExpendableProperties.IgnoreQueryFilters()
                on icsItem.SemiExpendablePropertyId equals prop.Id
            join catalogItem in dbContext.SemiExpendableItems
                on prop.SemiExpendableItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                on icsItem.ICSId equals ics.Id
            select new { ics, icsItem, prop, catalogItem };

        if (query.ActiveOnly)
        {
            q = q.Where(x => x.ics.Status == ICSStatus.Active);
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.ics.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.ics.Date <= query.DateTo.Value);
        }

        if (query.Category.HasValue)
        {
            q = q.Where(x => x.icsItem.CategoryAtTimeOfIssuance == query.Category.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await q
            .OrderBy(x => x.ics.Date)
            .ThenBy(x => x.prop.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RSPIItemDto(
                x.ics.Id,
                x.ics.ICSNo,
                x.ics.Date,
                x.ics.Status.ToString(),
                x.ics.ReceivedByEmployeeId,
                x.ics.IssuedFromEmployeeId,
                x.prop.Id,
                x.prop.PropertyNo,
                x.prop.SerialNo,
                x.catalogItem.Code,
                x.catalogItem.Name,
                x.icsItem.CategoryAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.ics.ExpiresOn))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedRSPIResponse(rows, pageNumber, pageSize, totalCount);
    }
}
