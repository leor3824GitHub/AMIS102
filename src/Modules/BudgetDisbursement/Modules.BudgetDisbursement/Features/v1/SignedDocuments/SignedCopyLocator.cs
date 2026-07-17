using AMIS.Framework.Core.Domain;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments;

/// <summary>
/// Read-only lookup of a document's inlined <see cref="SignedCopy"/> by (document type, id), dispatching to
/// the owning aggregate's DbSet. Shared by the Get (metadata) and Download slices.
/// </summary>
internal static class SignedCopyLocator
{
    public static async Task<SignedCopy?> FindAsync(
        BudgetDisbursementDbContext db, BudgetDisbursementDocumentType type, Guid documentId, CancellationToken ct)
    {
        switch (type)
        {
            case BudgetDisbursementDocumentType.DisbursementVoucher:
                return (await db.DisbursementVouchers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case BudgetDisbursementDocumentType.BudgetUtilizationRequest:
                return (await db.BudgetUtilizationRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            default:
                return null;
        }
    }
}
