using FSH.Modules.Expendable.Contracts.v1.Products;
using FSH.Modules.Expendable.Domain.Products;

namespace FSH.Modules.Expendable.Features.v1.Products;

internal static class ProductMapper
{
    internal static ProductDto ToProductDto(this Product product) =>
        new(
            product.Id,
            product.SKU,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.UnitOfMeasure,
            product.MinimumStockLevel,
            product.ReorderQuantity,
            product.ParentProductId,
            product.VariantName,
            product.Status.ToString(),
            product.CategoryId,
            product.SupplierId,
            product.ImageUrl,
            product.CreatedOnUtc,
            product.CreatedBy,
            product.LastModifiedOnUtc,
            product.LastModifiedBy);

    internal static ProductCatalogCardDto ToCatalogCardDto(this Product product, int quantityAvailable) =>
        new(
            product.Id,
            product.SKU,
            product.Name,
            product.UnitPrice,
            product.UnitOfMeasure,
            quantityAvailable,
            quantityAvailable > 0,
            product.Status.ToString(),
            product.CategoryId,
            product.ImageUrl,
            product.ParentProductId,
            product.VariantName);
}

