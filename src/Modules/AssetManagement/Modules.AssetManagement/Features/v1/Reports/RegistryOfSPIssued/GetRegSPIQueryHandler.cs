using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

public sealed class GetRegSPIQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRegSPIQuery, PagedRegSPIResponse>
{
    public async ValueTask<PagedRegSPIResponse> Handle(GetRegSPIQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        // Base: ICS items for this employee, joined to inventory item and catalog.
        var q =
            from icsItem in dbContext.ICSItems
            join inv in dbContext.TangibleInventoryItems.IgnoreQueryFilters()
                on icsItem.TangibleInventoryItemId equals inv.Id
            join catalogItem in dbContext.PropertyItemCatalog
                on inv.ItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                on icsItem.ICSId equals ics.Id
            where ics.ReceivedByEmployeeId == query.EmployeeId
            select new { ics, icsItem, inv, catalogItem };

        if (query.AssetType.HasValue)
        {
            q = q.Where(x => x.icsItem.AssetTypeAtTimeOfIssuance == query.AssetType.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.ics.Status == query.Status.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.ics.Date)
            .ThenBy(x => x.inv.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RegSPIEntryDto(
                x.ics.Id,
                x.ics.ICSNo,
                x.ics.Date,
                x.ics.FundCluster,
                x.inv.Id,
                x.inv.PropertyNo,
                x.catalogItem.Code,
                x.catalogItem.Name,
                x.icsItem.AssetTypeAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.icsItem.EstimatedUsefulLifeYears,
                x.ics.ExpiresOn,
                x.ics.Status.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedRegSPIResponse(query.EmployeeId, rows, pageNumber, pageSize, totalCount);
    }
}
