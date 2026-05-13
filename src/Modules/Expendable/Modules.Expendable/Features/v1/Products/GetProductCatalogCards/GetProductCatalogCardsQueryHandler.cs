using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductCatalogCards;

public sealed class GetProductCatalogCardsQueryHandler : IQueryHandler<GetProductCatalogCardsQuery, PagedResponse<ProductCatalogCardDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetProductCatalogCardsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ProductCatalogCardDto>> Handle(GetProductCatalogCardsQuery query, CancellationToken cancellationToken)
    {
        var productQuery = _dbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword}%";
            productQuery = productQuery.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.SKU, pattern) ||
                EF.Functions.ILike(p.Description, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            productQuery = productQuery.Where(p => p.CategoryId == query.CategoryId);
        }

        productQuery = productQuery.Where(p => p.Status == ProductStatus.Active || p.Status == ProductStatus.OutOfStock);

        var cardsQuery =
            from product in productQuery
            join inventory in _dbContext.ProductInventories.AsNoTracking()
                on product.Id equals inventory.ProductId into inventoryJoin
            from inventory in inventoryJoin.DefaultIfEmpty()
            select new ProductCatalogCardDto(
                product.Id,
                product.SKU,
                product.Name,
                product.UnitPrice,
                product.UnitOfMeasure,
                inventory != null ? inventory.QuantityAvailable : 0,
                (inventory != null ? inventory.QuantityAvailable : 0) > 0,
                product.Status.ToString(),
                product.CategoryId,
                product.ImageUrl,
                product.ParentProductId,
                product.VariantName);

        if (query.InStockOnly)
        {
            cardsQuery = cardsQuery.Where(c => c.IsInStock);
        }

        cardsQuery = cardsQuery.OrderBy(c => c.Name);

        return await cardsQuery.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

