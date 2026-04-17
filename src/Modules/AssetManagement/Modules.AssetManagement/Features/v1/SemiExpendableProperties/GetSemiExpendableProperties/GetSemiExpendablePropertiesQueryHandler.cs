using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendableProperties;

public sealed class GetSemiExpendablePropertiesQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSemiExpendablePropertiesQuery, PagedSemiExpendablePropertiesResponse>
{
    public async ValueTask<PagedSemiExpendablePropertiesResponse> Handle(GetSemiExpendablePropertiesQuery query, CancellationToken cancellationToken)
    {
        var propertiesQuery = dbContext.SemiExpendableProperties
            .Include(x => x.Item)
            .AsQueryable();

        // Exclude transferred properties from default view; caller must pass Status=Transferred to see them explicitly.
        if (!query.Status.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.Status != PropertyStatus.Transferred);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            propertiesQuery = propertiesQuery.Where(x =>
                x.PropertyNo.ToLower().Contains(kw) ||
                (x.SerialNo != null && x.SerialNo.ToLower().Contains(kw)) ||
                x.Item.Code.ToLower().Contains(kw) ||
                x.Item.Name.ToLower().Contains(kw));
        }

        if (query.ItemId.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.ItemId == query.ItemId.Value);
        }

        if (query.Category.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.Category == query.Category.Value);
        }

        if (query.Status.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.Status == query.Status.Value);
        }

        if (query.CurrentCustodianId.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.CurrentCustodianId == query.CurrentCustodianId.Value);
        }

        var totalCount = await propertiesQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var properties = await propertiesQuery
            .OrderBy(x => x.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SemiExpendablePropertySummaryDto(
                x.Id,
                x.PropertyNo,
                x.ItemId,
                x.Item.Code,
                x.Item.Name,
                x.Category.ToString(),
                x.SerialNo,
                x.AcquisitionDate,
                x.UnitCost,
                x.FundCluster,
                x.Status.ToString(),
                x.CurrentCustodianId,
                x.Remarks))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedSemiExpendablePropertiesResponse(properties, pageNumber, pageSize, totalCount);
    }
}
