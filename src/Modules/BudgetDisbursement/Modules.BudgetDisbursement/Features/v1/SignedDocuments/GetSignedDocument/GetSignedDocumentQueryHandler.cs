using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.GetSignedDocument;

public sealed class GetSignedDocumentQueryHandler(BudgetDisbursementDbContext dbContext)
    : IQueryHandler<GetSignedDocumentQuery, SignedDocumentDto?>
{
    public async ValueTask<SignedDocumentDto?> Handle(GetSignedDocumentQuery query, CancellationToken cancellationToken)
    {
        var d = await dbContext.SignedDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocumentType == query.DocumentType && x.DocumentId == query.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        return d is null
            ? null
            : new SignedDocumentDto(d.DocumentType, d.DocumentId, d.FileName, d.ContentType, d.FileSizeBytes, d.Sha256, d.UploadedByName, d.UploadedOnUtc);
    }
}
