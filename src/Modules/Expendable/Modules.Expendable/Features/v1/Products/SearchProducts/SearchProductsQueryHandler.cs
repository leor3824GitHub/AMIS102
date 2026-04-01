using FSH.Framework.Persistence;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Expendable.Contracts.v1.Products;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Products.SearchProducts;

public sealed class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, PagedResponse<ProductDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public SearchProductsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ProductDto>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var productQuery = _dbContext.Products.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword}%";
            productQuery = productQuery.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.SKU, pattern) ||
                EF.Functions.ILike(p.Description, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<ProductStatus>(query.Status, out var status))
        {
            productQuery = productQuery.Where(p => p.Status == status);
        }

        if (query.ParentProductId is not null)
        {
            productQuery = productQuery.Where(p => p.ParentProductId == query.ParentProductId);
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            productQuery = productQuery.Where(p => p.CategoryId == query.CategoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.SupplierId))
        {
            productQuery = productQuery.Where(p => p.SupplierId == query.SupplierId);
        }

        productQuery = productQuery.OrderBy(p => p.Name);

        var projected = productQuery.Select(p => p.ToProductDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}
