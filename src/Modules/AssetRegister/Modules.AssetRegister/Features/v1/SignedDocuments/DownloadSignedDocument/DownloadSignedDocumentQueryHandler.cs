using System.Security.Cryptography;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.DownloadSignedDocument;

public sealed class DownloadSignedDocumentQueryHandler(
    AssetRegisterDbContext db,
    IStorageService storageService,
    ILogger<DownloadSignedDocumentQueryHandler> logger)
    : IQueryHandler<DownloadSignedDocumentQuery, SignedDocumentFileDto?>
{
    public async ValueTask<SignedDocumentFileDto?> Handle(DownloadSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var row = await db.SignedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentType == query.DocumentType && x.DocumentId == query.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return null;

        var file = await storageService.DownloadAsync(row.StorageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            logger.LogError("Signed copy blob missing for {DocumentType} {DocumentId} (key {Key}).",
                row.DocumentType, row.DocumentId, row.StorageKey);
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
        if (!string.Equals(actual, row.Sha256, StringComparison.Ordinal))
        {
            logger.LogError("Integrity check FAILED for signed copy {DocumentType} {DocumentId}: expected {Expected}, got {Actual}.",
                row.DocumentType, row.DocumentId, row.Sha256, actual);
            throw new InvalidOperationException("The stored signed copy failed its integrity check and may be corrupted or tampered with.");
        }

        return new SignedDocumentFileDto(content, row.ContentType, row.FileName);
    }
}
