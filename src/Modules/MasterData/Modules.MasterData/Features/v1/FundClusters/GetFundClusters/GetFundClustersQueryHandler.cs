using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusters;

public sealed class GetFundClustersQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetFundClustersQuery, PagedResponseOfFundClusterDto>
{
    public async ValueTask<PagedResponseOfFundClusterDto> Handle(GetFundClustersQuery query, CancellationToken cancellationToken)
    {
        var entityQuery = dbContext.FundClusters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            entityQuery = entityQuery.Where(x =>
                x.Code.ToLower().Contains(keyword) ||
                x.Name.ToLower().Contains(keyword) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword)));
        }

        var totalCount = await entityQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var skipCount = (pageNumber - 1) * pageSize;

        var items = await entityQuery
            .OrderBy(x => x.Code)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(x => new FundClusterDto(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponseOfFundClusterDto(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }
}
