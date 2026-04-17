using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
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

        // Base: ICS items for this employee, joined to property and catalog.
        var q =
            from icsItem in dbContext.ICSItems
            join prop in dbContext.SemiExpendableProperties.IgnoreQueryFilters()
                on icsItem.SemiExpendablePropertyId equals prop.Id
            join catalogItem in dbContext.PropertyItemCatalog
                on prop.ItemId equals catalogItem.Id
            join ics in dbContext.InventoryCustodianSlips.IgnoreQueryFilters()
                on icsItem.ICSId equals ics.Id
            where ics.ReceivedByEmployeeId == query.EmployeeId
            select new { ics, icsItem, prop, catalogItem };

        if (query.Category.HasValue)
        {
            q = q.Where(x => x.icsItem.CategoryAtTimeOfIssuance == query.Category.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.ics.Status == query.Status.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.ics.Date)
            .ThenBy(x => x.prop.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RegSPIEntryDto(
                x.ics.Id,
                x.ics.ICSNo,
                x.ics.Date,
                x.ics.FundCluster,
                x.prop.Id,
                x.prop.PropertyNo,
                x.catalogItem.Code,
                x.catalogItem.Name,
                x.icsItem.CategoryAtTimeOfIssuance.ToString(),
                x.icsItem.UnitCost,
                x.icsItem.EstimatedUsefulLifeYears,
                x.ics.ExpiresOn,
                x.ics.Status.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedRegSPIResponse(query.EmployeeId, rows, pageNumber, pageSize, totalCount);
    }
}
