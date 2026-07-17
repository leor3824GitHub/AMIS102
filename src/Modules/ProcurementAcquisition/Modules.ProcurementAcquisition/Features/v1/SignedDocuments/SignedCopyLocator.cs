using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments;

/// <summary>
/// Read-only lookup of a document's inlined <see cref="SignedCopy"/> by (document type, id), dispatching to
/// the owning aggregate's DbSet. Shared by the Get (metadata) and Download slices. RFQ resolves against the
/// CanvassQuotation aggregate; InspectionAcceptanceReport currently has no upload path so its copy is null.
/// </summary>
internal static class SignedCopyLocator
{
    public static async Task<SignedCopy?> FindAsync(
        ProcurementDbContext db, ProcurementDocumentType type, Guid documentId, CancellationToken ct)
    {
        switch (type)
        {
            case ProcurementDocumentType.PurchaseRequest:
                return (await db.PurchaseRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case ProcurementDocumentType.PurchaseOrder:
                return (await db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case ProcurementDocumentType.JobOrder:
                return (await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case ProcurementDocumentType.AbstractOfCanvass:
                return (await db.CanvassRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case ProcurementDocumentType.RequestForQuotation:
                return (await db.CanvassQuotations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            case ProcurementDocumentType.InspectionAcceptanceReport:
                return (await db.InspectionAcceptanceReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentId, ct).ConfigureAwait(false))?.SignedCopy;
            default:
                return null;
        }
    }
}
