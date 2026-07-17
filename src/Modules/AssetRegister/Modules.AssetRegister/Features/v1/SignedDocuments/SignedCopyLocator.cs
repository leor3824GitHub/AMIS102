using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments;

/// <summary>
/// Read-only lookup of a document's inlined <see cref="SignedCopy"/> by (document type, id), dispatching to
/// the owning aggregate's DbSet. Shared by the Get (metadata) and Download slices.
/// </summary>
internal static class SignedCopyLocator
{
    public static async Task<SignedCopy?> FindAsync(
        AssetRegisterDbContext db, AssetRegisterDocumentType type, Guid documentId, CancellationToken ct)
    {
        switch (type)
        {
            case AssetRegisterDocumentType.ReturnedPropertyReceipt:
                return (await db.ReturnedPropertyReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.PropertyAccountability:
                return (await db.PropertyAccountabilities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.IssuanceReport:
                return (await db.PropertyIssuanceReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.ReceivingReport:
                return (await db.ReceivingReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.UnserviceableReport:
                return (await db.UnserviceablePropertyReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.IncidentReport:
                return (await db.PropertyIncidentReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case AssetRegisterDocumentType.PhysicalCountReport:
                return (await db.PhysicalCountSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            default:
                return null;
        }
    }
}
