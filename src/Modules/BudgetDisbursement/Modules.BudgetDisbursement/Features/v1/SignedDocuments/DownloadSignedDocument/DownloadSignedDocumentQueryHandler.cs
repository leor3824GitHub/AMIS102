using System.Net;
using System.Security.Cryptography;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.DownloadSignedDocument;

public sealed class DownloadSignedDocumentQueryHandler(
    BudgetDisbursementDbContext dbContext,
    IStorageService storageService,
    ICurrentUser currentUser,
    ILogger<DownloadSignedDocumentQueryHandler> logger)
    : IQueryHandler<DownloadSignedDocumentQuery, SignedDocumentFileDto?>
{
    public async ValueTask<SignedDocumentFileDto?> Handle(DownloadSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? string.Empty;

        // Load the signed document row and the module settings in parallel.
        var rowTask = dbContext.SignedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentType == query.DocumentType && x.DocumentId == query.DocumentId, cancellationToken);

        var settingsTask = dbContext.BudgetDisbursementSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        await Task.WhenAll(rowTask, settingsTask).ConfigureAwait(false);

        var row = await rowTask;
        if (row is null)
            return null;

        var file = await storageService.DownloadAsync(row.StorageKey, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            logger.LogError("Signed copy blob missing for {DocumentType} {DocumentId} (key {Key}).",
                row.DocumentType, row.DocumentId, row.StorageKey);
            throw new CustomException("The stored signed copy could not be retrieved.",
                Enumerable.Empty<string>(), HttpStatusCode.InternalServerError);
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
            throw new CustomException("The stored signed copy failed its integrity check and may be corrupted or tampered with.",
                Enumerable.Empty<string>(), HttpStatusCode.InternalServerError);
        }

        // Apply watermark only when the admin setting is enabled (defaults to true if no row yet).
        var settings = await settingsTask;
        var watermarkEnabled = settings?.WatermarkSignedCopies ?? true;

        if (watermarkEnabled)
        {
            var downloaderName = currentUser.Name ?? currentUser.GetUserId().ToString();
            content = PdfWatermarkService.Stamp(content, downloaderName, DateTimeOffset.UtcNow);
        }

        return new SignedDocumentFileDto(content, row.ContentType, row.FileName);
    }
}
