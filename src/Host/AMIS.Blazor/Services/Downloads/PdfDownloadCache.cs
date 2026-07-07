using Microsoft.Extensions.Caching.Memory;

namespace AMIS.Blazor.Services.Downloads;

/// <summary>
/// Short-lived, per-user hand-off store for generated PDF bytes.
///
/// Blazor Server pages generate a PDF server-side (in the circuit) and previously shipped it to the
/// browser as a Base64 data URL over the SignalR circuit (+33% size, uncompressed, marshalled through
/// the socket). Instead we stash the bytes here under a random token and hand the browser only the token;
/// the browser then fetches the PDF over a normal HTTP request. Entries are one-shot and expire quickly.
/// </summary>
internal interface IPdfDownloadCache
{
    /// <summary>Stores content and returns a random single-use token.</summary>
    string Store(byte[] content, string fileName, string contentType, string ownerUserId);

    /// <summary>Removes and returns the entry if the token exists and belongs to <paramref name="ownerUserId"/>.</summary>
    PdfDownloadEntry? Take(string token, string ownerUserId);
}

internal sealed record PdfDownloadEntry(byte[] Content, string FileName, string ContentType, string OwnerUserId);

internal sealed class PdfDownloadCache : IPdfDownloadCache, IDisposable
{
    // Bytes live only for the brief moment between the circuit generating them and the browser fetching them.
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    // Dedicated cache instance so PDF payloads never contend with the app's shared IMemoryCache.
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public string Store(byte[] content, string fileName, string contentType, string ownerUserId)
    {
        ArgumentNullException.ThrowIfNull(content);

        var token = Guid.NewGuid().ToString("N");
        _cache.Set(
            token,
            new PdfDownloadEntry(content, fileName, contentType, ownerUserId),
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl });
        return token;
    }

    public PdfDownloadEntry? Take(string token, string ownerUserId)
    {
        if (string.IsNullOrEmpty(token) || !_cache.TryGetValue(token, out PdfDownloadEntry? entry) || entry is null)
        {
            return null;
        }

        // One-shot: remove immediately so a token cannot be replayed.
        _cache.Remove(token);

        // A token only serves the user who created it.
        if (string.IsNullOrEmpty(ownerUserId) ||
            !string.Equals(entry.OwnerUserId, ownerUserId, StringComparison.Ordinal))
        {
            return null;
        }

        return entry;
    }

    public void Dispose() => _cache.Dispose();
}
