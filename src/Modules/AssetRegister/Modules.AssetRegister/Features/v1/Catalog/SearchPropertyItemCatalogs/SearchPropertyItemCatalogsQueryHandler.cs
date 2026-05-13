using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Catalog.SearchPropertyItemCatalogs;

public sealed class SearchPropertyItemCatalogsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<SearchPropertyItemCatalogsQuery, PagedResponse<PropertyItemCatalogDto>>
{
    public async ValueTask<PagedResponse<PropertyItemCatalogDto>> Handle(
        SearchPropertyItemCatalogsQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.PropertyItemCatalogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var k = query.Keyword.ToLowerInvariant();
            q = q.Where(x =>
                x.Code.ToLower().Contains(k) ||
                x.Description.ToLower().Contains(k));
        }
        if (query.IsActive.HasValue)
            q = q.Where(x => x.IsActive == query.IsActive.Value);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var total = await q.LongCountAsync(ct).ConfigureAwait(false);
        var items = await q.OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new PropertyItemCatalogDto(
                x.Id, x.Code, x.Description, x.DefaultPropertyClass, x.DefaultCategoryCode,
                x.DefaultUnit, x.UacsObjectCode, x.EstimatedUsefulLifeYears, x.IsActive))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResponse<PropertyItemCatalogDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

