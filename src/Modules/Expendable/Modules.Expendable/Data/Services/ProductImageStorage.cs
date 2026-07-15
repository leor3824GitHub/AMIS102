using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.Expendable.Domain.Products;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AMIS.Modules.Expendable.Data.Services;

/// <summary>Decoded image bytes plus the content type to serve them with.</summary>
public sealed record ProductImageResult(byte[] Content, string ContentType);

/// <summary>
/// Owns product-photo persistence: writes the full image + a small thumbnail as files under the
/// tenant-scoped <c>protected</c> prefix (served only through the permission-gated image endpoint,
/// never as anonymous static content), and resolves a stored value back to bytes. Stored
/// <c>Product.ImageUrl</c>/<c>ThumbnailUrl</c> hold short storage keys; a legacy <c>data:…;base64,</c>
/// value is still decoded transparently so pre-migration rows keep rendering. Mirrors
/// <c>AssetRegister.Data.Services.AssetImageStorage</c>.
/// </summary>
public sealed class ProductImageStorage(IStorageService storage)
{
    // Full image is downscaled to this longest edge; the catalog/list thumbnail is downscaled to a
    // ~square this size — serves both the 48px products-list rows and the ~140px shopping cards crisply.
    private const int FullMaxEdge = 1600;
    private const int ThumbnailMaxEdge = 200;
    private const string JpegContentType = "image/jpeg";

    /// <summary>
    /// Normalizes the uploaded bytes to JPEG, writing the (downscaled) full image and a thumbnail, and
    /// returns their storage keys. CPU-bound decode/resize/encode runs on a worker thread.
    /// </summary>
    public async Task<(string ImageKey, string ThumbnailKey)> SaveAsync(
        byte[] bytes, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var (fullJpeg, thumbJpeg) = await Task.Run(() => Encode(bytes), cancellationToken).ConfigureAwait(false);

        var folder = StoragePaths.Protected(tenantId, "product-images");
        var imageKey = await UploadJpegAsync(fullJpeg, "photo.jpg", folder, cancellationToken).ConfigureAwait(false);
        try
        {
            var thumbKey = await UploadJpegAsync(thumbJpeg, "thumb.jpg", folder, cancellationToken).ConfigureAwait(false);
            return (imageKey, thumbKey);
        }
        catch
        {
            // Don't orphan the full image if the thumbnail write fails.
            try { await storage.RemoveAsync(imageKey, cancellationToken).ConfigureAwait(false); }
            catch { /* best effort */ }
            throw;
        }
    }

    /// <summary>Resolves a stored value (storage key or legacy base64 data URL) to bytes, or null.</summary>
    public async Task<ProductImageResult?> LoadAsync(string? storedValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return null;

        if (storedValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return DecodeDataUrl(storedValue);

        var file = await storage.DownloadAsync(storedValue, cancellationToken).ConfigureAwait(false);
        if (file is null)
            return null;

        await using (file.Stream)
        {
            using var ms = new MemoryStream();
            await file.Stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            return new ProductImageResult(ms.ToArray(), file.ContentType);
        }
    }

    /// <summary>Best-effort removal of the full + thumbnail files for a replaced/cleared photo.</summary>
    public async Task RemoveAsync(string? imageKey, string? thumbnailKey, CancellationToken cancellationToken)
    {
        foreach (var key in new[] { imageKey, thumbnailKey })
        {
            // A legacy base64 value isn't a file — nothing to delete.
            if (string.IsNullOrWhiteSpace(key) || key.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;
            try { await storage.RemoveAsync(key, cancellationToken).ConfigureAwait(false); }
            catch { /* orphaned blob is harmless */ }
        }
    }

    private async Task<string> UploadJpegAsync(byte[] jpeg, string fileName, string folder, CancellationToken ct) =>
        await storage.UploadAsync<Product>(
            new FileUploadRequest { FileName = fileName, ContentType = JpegContentType, Data = [.. jpeg] },
            FileType.Image, folder, ct).ConfigureAwait(false);

    private static (byte[] Full, byte[] Thumb) Encode(byte[] bytes)
    {
        using var image = Image.Load(bytes);
        Downscale(image, FullMaxEdge);
        var full = ToJpeg(image, 90);

        Downscale(image, ThumbnailMaxEdge);
        var thumb = ToJpeg(image, 80);
        return (full, thumb);
    }

    private static void Downscale(Image image, int maxEdge)
    {
        if (image.Width <= maxEdge && image.Height <= maxEdge)
            return;
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxEdge, maxEdge)
        }));
    }

    private static byte[] ToJpeg(Image image, int quality)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = quality });
        return ms.ToArray();
    }

    private static ProductImageResult? DecodeDataUrl(string value)
    {
        var base64Marker = value.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
        if (base64Marker < 0)
            return null;

        var contentType = value[5..base64Marker].TrimEnd(';');
        if (string.IsNullOrWhiteSpace(contentType))
            contentType = JpegContentType;

        try
        {
            var bytes = Convert.FromBase64String(value[(base64Marker + "base64,".Length)..]);
            return bytes.Length == 0 ? null : new ProductImageResult(bytes, contentType);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
