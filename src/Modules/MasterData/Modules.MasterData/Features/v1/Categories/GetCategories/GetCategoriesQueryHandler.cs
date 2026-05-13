using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Categories.GetCategories;

public sealed class GetCategoriesQueryHandler(MasterDataDbContext dbContext) : IQueryHandler<GetCategoriesQuery, PagedResponseOfCategoryDto>
{
    public async ValueTask<PagedResponseOfCategoryDto> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var categoriesQuery = dbContext.Categories.AsQueryable();

        // Apply keyword filter if provided
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.ToLower();
            categoriesQuery = categoriesQuery.Where(c =>
                c.Code.ToLower().Contains(keyword) ||
                c.Name.ToLower().Contains(keyword) ||
                (c.Description != null && c.Description.ToLower().Contains(keyword)));
        }

        // Get total count before pagination
        var totalCount = await categoriesQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply pagination
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var skipCount = (pageNumber - 1) * pageSize;

        var categories = await categoriesQuery
            .OrderBy(c => c.Code)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(c => new CategoryDto(
                c.Id,
                c.Code,
                c.Name,
                c.Description,
                c.IsActive,
                c.OfficeCode))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponseOfCategoryDto(
            categories,
            pageNumber,
            pageSize,
            totalCount);
    }
}

