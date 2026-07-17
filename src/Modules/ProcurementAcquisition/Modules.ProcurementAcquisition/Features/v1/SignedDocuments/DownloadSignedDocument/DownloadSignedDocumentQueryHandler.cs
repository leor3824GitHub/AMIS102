using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.DownloadSignedDocument;

public sealed class DownloadSignedDocumentQueryHandler(
    ProcurementDbContext dbContext,
    IStorageService storageService,
    ILogger<DownloadSignedDocumentQueryHandler> logger)
    : IQueryHandler<DownloadSignedDocumentQuery, SignedDocumentFileDto?>
{
    public async ValueTask<SignedDocumentFileDto?> Handle(DownloadSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        var copy = await SignedCopyLocator.FindAsync(dbContext, query.DocumentType, query.DocumentId, cancellationToken).ConfigureAwait(false);
        if (copy is null)
            return null;

        var file = await SignedCopyStore.DownloadAsync(storageService, logger, copy, cancellationToken).ConfigureAwait(false);
        return new SignedDocumentFileDto(file.Content, file.ContentType, file.FileName);
    }
}
