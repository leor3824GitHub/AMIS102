using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.UpdateAssetImage;

public sealed class UpdateAssetImageCommandHandler(AssetRegisterDbContext db, AssetImageStorage imageStorage)
    : ICommandHandler<UpdateAssetImageCommand, AssetRegistryDto>
{
    public async ValueTask<AssetRegistryDto> Handle(UpdateAssetImageCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == cmd.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{cmd.AssetRegistryId}' not found.");

        // Remember the previous files so they can be cleaned up after the row commits.
        var oldImageKey = asset.ImageUrl;
        var oldThumbnailKey = asset.ThumbnailUrl;

        if (string.IsNullOrWhiteSpace(cmd.ImageUrl))
        {
            asset.ClearImage();
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await imageStorage.RemoveAsync(oldImageKey, oldThumbnailKey, cancellationToken).ConfigureAwait(false);
            return AssetRegistryMapper.ToDto(asset);
        }

        // The validator guarantees a well-formed image data URL. Decode → write full + thumbnail files.
        var bytes = DecodeDataUrl(cmd.ImageUrl)
            ?? throw new InvalidOperationException("The provided image is not a valid base64 data URL.");
        var (imageKey, thumbnailKey) = await imageStorage.SaveAsync(bytes, asset.TenantId, cancellationToken).ConfigureAwait(false);

        asset.SetImage(imageKey, thumbnailKey);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Row didn't persist → don't orphan the blobs we just wrote.
            await imageStorage.RemoveAsync(imageKey, thumbnailKey, cancellationToken).ConfigureAwait(false);
            throw;
        }

        // Best-effort cleanup of the replaced photo (after the commit succeeded).
        await imageStorage.RemoveAsync(oldImageKey, oldThumbnailKey, cancellationToken).ConfigureAwait(false);
        return AssetRegistryMapper.ToDto(asset);
    }

    private static byte[]? DecodeDataUrl(string value)
    {
        var marker = value.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return null;
        try { return Convert.FromBase64String(value[(marker + "base64,".Length)..]); }
        catch (FormatException) { return null; }
    }
}
