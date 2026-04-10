using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendableProperties;

public sealed class GetSemiExpendablePropertiesQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSemiExpendablePropertiesQuery, PagedSemiExpendablePropertiesResponse>
{
    public async ValueTask<PagedSemiExpendablePropertiesResponse> Handle(GetSemiExpendablePropertiesQuery query, CancellationToken cancellationToken)
    {
        var propertiesQuery = dbContext.SemiExpendableProperties
            .Include(x => x.SemiExpendableItem)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            propertiesQuery = propertiesQuery.Where(x =>
                x.PropertyNo.ToLower().Contains(kw) ||
                (x.SerialNo != null && x.SerialNo.ToLower().Contains(kw)) ||
                x.SemiExpendableItem.Code.ToLower().Contains(kw) ||
                x.SemiExpendableItem.Name.ToLower().Contains(kw));
        }

        if (query.SemiExpendableItemId.HasValue)
        {
            propertiesQuery = propertiesQuery.Where(x => x.SemiExpendableItemId == query.SemiExpendableItemId.Value);
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
                x.SemiExpendableItemId,
                x.SemiExpendableItem.Code,
                x.SemiExpendableItem.Name,
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
