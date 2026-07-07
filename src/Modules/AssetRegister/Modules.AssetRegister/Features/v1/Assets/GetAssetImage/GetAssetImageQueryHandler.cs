using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetAssetImage;

/// <summary>
/// Serves the asset photo behind the lazy image endpoint. Images are stored as files under the
/// tenant-scoped protected prefix; this projects only the relevant key (thumbnail for lists, full for
/// detail) and streams the file via <see cref="AssetImageStorage"/> — which also transparently decodes
/// any pre-migration base64 value. No full-entity materialization.
/// </summary>
public sealed class GetAssetImageQueryHandler(AssetRegisterDbContext db, AssetImageStorage imageStorage)
    : IQueryHandler<GetAssetImageQuery, AssetImageResult?>
{
    public async ValueTask<AssetImageResult?> Handle(GetAssetImageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var keys = await db.AssetRegistries.AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(a => new { a.ImageUrl, a.ThumbnailUrl })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (keys is null)
            return null;

        // Prefer the thumbnail for list rows; fall back to the full image when a thumbnail was never
        // generated (e.g. a legacy base64 row that only has ImageUrl).
        var stored = query.Variant == AssetImageVariant.Thumbnail
            ? keys.ThumbnailUrl ?? keys.ImageUrl
            : keys.ImageUrl;

        return await imageStorage.LoadAsync(stored, cancellationToken).ConfigureAwait(false);
    }
}
