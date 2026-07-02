using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodes;

public sealed class GetFundingSourceCodesQueryHandler(MasterDataDbContext dbContext)
    : IQueryHandler<GetFundingSourceCodesQuery, PagedResponseOfFundingSourceCodeDto>
{
    public async ValueTask<PagedResponseOfFundingSourceCodeDto> Handle(GetFundingSourceCodesQuery query, CancellationToken cancellationToken)
    {
        var entityQuery = dbContext.FundingSourceCodes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.FundClusterCode))
        {
            entityQuery = entityQuery.Where(x => x.FundClusterCode == query.FundClusterCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            entityQuery = entityQuery.Where(x =>
                x.Code.ToLower().Contains(keyword) ||
                (x.Description != null && x.Description.ToLower().Contains(keyword)) ||
                (x.FundCategory != null && x.FundCategory.ToLower().Contains(keyword)) ||
                (x.FundSubCategory != null && x.FundSubCategory.ToLower().Contains(keyword)));
        }

        var totalCount = await entityQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var skipCount = (pageNumber - 1) * pageSize;

        var items = await entityQuery
            .OrderBy(x => x.Code)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(x => new FundingSourceCodeDto(
                x.Id,
                x.Code,
                x.FundClusterCode,
                x.FinancingSource,
                x.Authorization,
                x.FundCategory,
                x.FundSubCategory,
                x.Description,
                x.DepartmentName,
                x.AgencyName,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponseOfFundingSourceCodeDto(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }
}
