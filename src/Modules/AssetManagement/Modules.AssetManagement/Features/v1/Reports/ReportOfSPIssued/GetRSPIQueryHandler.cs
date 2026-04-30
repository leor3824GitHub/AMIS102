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
            join inv in dbContext.TangibleInventoryItems
                on icsItem.TangibleInventoryItemId equals inv.Id
            join catalogItem in dbContext.PropertyItemCatalog
                on inv.ItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips
                on icsItem.ICSId equals ics.Id
            select new { ics, icsItem, inv, catalogItem };

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

        if (query.AssetType.HasValue)
        {
            q = q.Where(x => x.icsItem.AssetTypeAtTimeOfIssuance == query.AssetType.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await q
            .OrderBy(x => x.ics.Date)
            .ThenBy(x => x.inv.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RSPIItemDto(
                x.ics.Id,
                x.ics.ICSNo,
                x.ics.Date,
                x.ics.Status.ToString(),
                x.ics.ReceivedByEmployeeId,
                x.ics.IssuedFromEmployeeId,
                x.inv.Id,
                x.inv.PropertyNo,
                x.catalogItem.Code,
                x.catalogItem.Name,
                x.icsItem.AssetTypeAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.ics.ExpiresOn))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedRSPIResponse(rows, pageNumber, pageSize, totalCount);
    }
}
