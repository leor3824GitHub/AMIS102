using AMIS.Framework.Core.Context;
using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
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
        var copy = await SignedCopyLocator.FindAsync(dbContext, query.DocumentType, query.DocumentId, cancellationToken).ConfigureAwait(false);
        if (copy is null)
            return null;

        var tenantId = currentUser.GetTenant() ?? string.Empty;
        var settings = await dbContext.BudgetDisbursementSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        var file = await SignedCopyStore.DownloadAsync(storageService, logger, copy, cancellationToken).ConfigureAwait(false);
        var content = file.Content;

        // Apply watermark only when the admin setting is enabled (defaults to true if no row yet).
        var watermarkEnabled = settings?.WatermarkSignedCopies ?? true;
        if (watermarkEnabled)
        {
            var downloaderName = currentUser.Name ?? currentUser.GetUserId().ToString();
            content = PdfWatermarkService.Stamp(content, downloaderName, DateTimeOffset.UtcNow);
        }

        return new SignedDocumentFileDto(content, file.ContentType, file.FileName);
    }
}
