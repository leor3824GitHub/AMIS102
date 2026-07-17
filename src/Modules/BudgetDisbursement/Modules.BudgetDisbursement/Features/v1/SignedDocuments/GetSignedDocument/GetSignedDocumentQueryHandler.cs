using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.UploadSignedDocument;
using Mediator;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.GetSignedDocument;

public sealed class GetSignedDocumentQueryHandler(BudgetDisbursementDbContext dbContext)
    : IQueryHandler<GetSignedDocumentQuery, SignedDocumentDto?>
{
    public async ValueTask<SignedDocumentDto?> Handle(GetSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        var copy = await SignedCopyLocator.FindAsync(dbContext, query.DocumentType, query.DocumentId, cancellationToken).ConfigureAwait(false);
        return copy is null ? null : UploadSignedDocumentCommandHandler.ToDto(query.DocumentType, query.DocumentId, copy);
    }
}
