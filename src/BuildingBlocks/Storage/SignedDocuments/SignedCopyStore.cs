using System.Security.Cryptography;
using AMIS.Framework.Core.Domain;
using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage.Services;
using Microsoft.Extensions.Logging;

namespace AMIS.Framework.Storage.SignedDocuments;

/// <summary>
/// Stateless mechanics shared by every module's signed-copy upload/download slice (mirrors the static
/// <c>SequenceAllocator</c> — no DI). Callers pass their own <see cref="IStorageService"/>. Signed copies are
/// PDF-only, so the content type is the <c>"application/pdf"</c> constant throughout.
/// </summary>
public static class SignedCopyStore
{
    private const string PdfContentType = "application/pdf";

    /// <summary>
    /// Hashes <paramref name="content"/>, uploads it to the tenant's protected signed-copy folder
    /// (<c>uploads/protected/{tenant}/{documentTypeFolder}</c> — never served as anonymous static content) and
    /// returns the resulting <see cref="SignedCopy"/> value. Persists nothing — the caller attaches the
    /// returned value to its aggregate and saves. On a failed save the caller should
    /// <see cref="IStorageService.RemoveAsync"/> the returned <see cref="SignedCopy.StorageKey"/>.
    /// </summary>
    public static async Task<SignedCopy> BuildAsync(
        IStorageService storage,
        string tenantId,
        string documentTypeFolder,
        byte[] content,
        string fileName,
        string? uploadedByName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(content);

        var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        var uploadRequest = new FileUploadRequest
        {
            FileName = fileName,
            ContentType = PdfContentType,
            Data = [.. content],
        };

        var storageKey = await storage
            .UploadAsync<SignedCopy>(uploadRequest, FileType.Pdf, StoragePaths.Protected(tenantId, documentTypeFolder), cancellationToken)
            .ConfigureAwait(false);

        return new SignedCopy(storageKey, sha256, fileName, content.LongLength, uploadedByName, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Fetches the stored blob for <paramref name="copy"/> and re-verifies its SHA-256 integrity before
    /// returning the bytes with the PDF content type. Throws if the blob is missing or fails the integrity check.
    /// </summary>
    public static async Task<SignedCopyFile> DownloadAsync(
        IStorageService storage,
        ILogger logger,
        SignedCopy copy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(copy);

        var file = await storage.DownloadAsync(copy.StorageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            logger.LogError("Signed copy blob missing (key {Key}).", copy.StorageKey);
            throw new InvalidOperationException("The stored signed copy could not be retrieved.");
        }

        byte[] content;
        await using (file.Stream)
        {
            using var ms = new MemoryStream();
            await file.Stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            content = ms.ToArray();
        }

        // Integrity check: the stored file must hash to what we recorded at upload.
        var actual = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        if (!string.Equals(actual, copy.Sha256, StringComparison.Ordinal))
        {
            logger.LogError("Integrity check FAILED for signed copy (key {Key}): expected {Expected}, got {Actual}.",
                copy.StorageKey, copy.Sha256, actual);
            throw new InvalidOperationException("The stored signed copy failed its integrity check and may be corrupted or tampered with.");
        }

        return new SignedCopyFile(content, PdfContentType, copy.FileName);
    }
}

/// <summary>The materialized bytes of a signed copy plus the content type/name for an HTTP file result.</summary>
public sealed record SignedCopyFile(byte[] Content, string ContentType, string FileName);
