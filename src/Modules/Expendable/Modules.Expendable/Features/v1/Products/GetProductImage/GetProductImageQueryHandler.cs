using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductImage;

/// <summary>
/// Serves the product photo behind the lazy image endpoint. Images are stored as files under the
/// tenant-scoped protected prefix; this projects only the relevant key (thumbnail for lists, full for
/// detail) and streams the file via <see cref="ProductImageStorage"/> — which also transparently decodes
/// any pre-migration base64 value. No full-entity materialization. Mirrors the AssetRegister handler.
/// </summary>
public sealed class GetProductImageQueryHandler(ExpendableDbContext db, ProductImageStorage imageStorage)
    : IQueryHandler<GetProductImageQuery, ProductImageResult?>
{
    public async ValueTask<ProductImageResult?> Handle(GetProductImageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var keys = await db.Products.AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new { p.ImageUrl, p.ThumbnailUrl })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (keys is null)
            return null;

        // Prefer the thumbnail for list rows; fall back to the full image when a thumbnail was never
        // generated (e.g. a legacy base64 row that only has ImageUrl).
        var stored = query.Variant == ProductImageVariant.Thumbnail
            ? keys.ThumbnailUrl ?? keys.ImageUrl
            : keys.ImageUrl;

        return await imageStorage.LoadAsync(stored, cancellationToken).ConfigureAwait(false);
    }
}
