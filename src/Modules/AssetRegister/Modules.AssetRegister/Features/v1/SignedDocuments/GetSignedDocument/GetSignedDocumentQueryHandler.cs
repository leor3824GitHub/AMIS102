using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.UploadSignedDocument;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.GetSignedDocument;

public sealed class GetSignedDocumentQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetSignedDocumentQuery, SignedDocumentDto?>
{
    public async ValueTask<SignedDocumentDto?> Handle(GetSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var row = await db.SignedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentType == query.DocumentType && x.DocumentId == query.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : UploadSignedDocumentCommandHandler.ToDto(row);
    }
}
