using AMIS.Modules.Expendable.Data.Services;
using Mediator;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductImage;

/// <summary>Which stored size of a product's photo to serve.</summary>
public enum ProductImageVariant
{
    Full,
    Thumbnail
}

/// <summary>Serves a product's photo bytes (thumbnail for list rows, full for detail).</summary>
public sealed record GetProductImageQuery(Guid Id, ProductImageVariant Variant)
    : IQuery<ProductImageResult?>;
