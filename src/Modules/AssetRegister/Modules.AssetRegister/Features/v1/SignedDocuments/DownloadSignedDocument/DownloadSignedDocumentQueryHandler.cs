using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
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

        var copy = await SignedCopyLocator.FindAsync(db, query.DocumentType, query.DocumentId, cancellationToken).ConfigureAwait(false);
        if (copy is null)
            return null;

        var file = await SignedCopyStore.DownloadAsync(storageService, logger, copy, cancellationToken).ConfigureAwait(false);
        return new SignedDocumentFileDto(file.Content, file.ContentType, file.FileName);
    }
}
