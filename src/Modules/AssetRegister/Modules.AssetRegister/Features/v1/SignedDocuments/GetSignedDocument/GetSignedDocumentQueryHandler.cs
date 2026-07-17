using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.UploadSignedDocument;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.GetSignedDocument;

public sealed class GetSignedDocumentQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetSignedDocumentQuery, SignedDocumentDto?>
{
    public async ValueTask<SignedDocumentDto?> Handle(GetSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var copy = await SignedCopyLocator.FindAsync(db, query.DocumentType, query.DocumentId, cancellationToken).ConfigureAwait(false);
        return copy is null ? null : UploadSignedDocumentCommandHandler.ToDto(query.DocumentType, query.DocumentId, copy);
    }
}
