using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Domain;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.UploadSignedDocument;

public sealed class UploadSignedDocumentCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IStorageService storageService,
    IMediator mediator) : ICommandHandler<UploadSignedDocumentCommand, SignedDocumentDto>
{
    public async ValueTask<SignedDocumentDto> Handle(UploadSignedDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Content is null || command.Content.Length == 0)
            throw new CustomException("Uploaded file is empty.", Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        // The signed copy may only be attached once the document has reached its terminal signed state.
        // Loaded tracked so the inlined SignedCopy is persisted with the aggregate on SaveChanges.
        var holder = await LoadSignableDocumentAsync(command.DocumentType, command.DocumentId, cancellationToken).ConfigureAwait(false);

        var tenantId = currentUser.GetTenant() ?? dbContext.TenantInfo?.Identifier ?? string.Empty;
        var oldKey = holder.SignedCopy?.StorageKey;

        var uploader = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        var copy = await SignedCopyStore.BuildAsync(
            storageService, tenantId, command.DocumentType.ToString(), command.Content,
            command.FileName, uploader.Name, cancellationToken).ConfigureAwait(false);

        holder.SetSignedCopy(copy);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The row didn't persist — don't leave the blob we just uploaded orphaned in storage.
            try { await storageService.RemoveAsync(copy.StorageKey, cancellationToken).ConfigureAwait(false); }
            catch (Exception) { /* best effort */ }
            throw;
        }

        // Best-effort cleanup of the replaced blob (after the row commit succeeded).
        if (oldKey is not null && !string.Equals(oldKey, copy.StorageKey, StringComparison.Ordinal))
        {
            try { await storageService.RemoveAsync(oldKey, cancellationToken).ConfigureAwait(false); }
            catch (Exception) { /* orphaned blob is harmless; the row is correct */ }
        }

        return ToDto(command.DocumentType, command.DocumentId, copy);
    }

    private async ValueTask<ISignedCopyHolder> LoadSignableDocumentAsync(ProcurementDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case ProcurementDocumentType.PurchaseRequest:
                var pr = await dbContext.PurchaseRequests
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Purchase request '{id}' not found.");
                // Signed at approval; remains a signed record through Completed.
                if (pr.Status is not (PurchaseRequestStatus.Approved or PurchaseRequestStatus.Completed))
                    throw new CustomException("A signed copy can only be uploaded once the purchase request is Approved.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return pr;

            case ProcurementDocumentType.PurchaseOrder:
                var po = await dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Purchase order '{id}' not found.");
                // Signed at issue; remains a signed record while deliveries are recorded (Partially/Fulfilled).
                if (po.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyDelivered or PurchaseOrderStatus.Fulfilled))
                    throw new CustomException("A signed copy can only be uploaded once the purchase order is Issued.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return po;

            case ProcurementDocumentType.JobOrder:
                var jo = await dbContext.JobOrders
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Job order '{id}' not found.");
                // Signed at issue; remains a signed record through inspection and acceptance.
                if (jo.Status is not (JobOrderStatus.Issued or JobOrderStatus.Inspected or JobOrderStatus.Completed))
                    throw new CustomException("A signed copy can only be uploaded once the job order is Issued.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return jo;

            case ProcurementDocumentType.AbstractOfCanvass:
                var canvass = await dbContext.CanvassRequests
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass request '{id}' not found.");
                if (canvass.Status != CanvassRequestStatus.Awarded)
                    throw new CustomException("A signed copy can only be uploaded for an Awarded canvass.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return canvass;

            case ProcurementDocumentType.RequestForQuotation:
                // The RFQ is the supplier's own wet-signed quotation document — the SignedCopy lives on the
                // CanvassQuotation. It may be attached at any non-cancelled canvass stage (unlike the Abstract
                // of Canvass, which summarises all quotations and is only signed once awarded).
                var quotation = await dbContext.CanvassQuotations
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass quotation '{id}' not found.");
                var parentCanvass = await dbContext.CanvassRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == quotation.CanvassRequestId, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass request '{quotation.CanvassRequestId}' not found.");
                if (parentCanvass.Status == CanvassRequestStatus.Cancelled)
                    throw new CustomException("A signed RFQ cannot be uploaded for a cancelled canvass.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return quotation;

            default:
                throw new CustomException($"Unsupported document type '{type}'.",
                    Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
        }
    }

    internal static SignedDocumentDto ToDto(ProcurementDocumentType type, Guid documentId, SignedCopy copy) => new(
        type, documentId, copy.FileName, "application/pdf", copy.FileSizeBytes, copy.Sha256, copy.UploadedByName, copy.UploadedOnUtc);
}
