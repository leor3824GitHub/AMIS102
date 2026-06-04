using System.Net;
using System.Security.Cryptography;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.SignedDocuments;
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
        await EnsureDocumentSignedAsync(command.DocumentType, command.DocumentId, cancellationToken).ConfigureAwait(false);

        var sha256 = Convert.ToHexString(SHA256.HashData(command.Content)).ToLowerInvariant();

        var uploadRequest = new FileUploadRequest
        {
            FileName = command.FileName,
            ContentType = command.ContentType,
            Data = [.. command.Content]
        };
        var storageKey = await storageService
            .UploadAsync<SignedDocument>(uploadRequest, FileType.Pdf, cancellationToken)
            .ConfigureAwait(false);

        var uploader = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        var uploaderId = currentUser.GetUserId();

        var existing = await dbContext.SignedDocuments
            .FirstOrDefaultAsync(x => x.DocumentType == command.DocumentType && x.DocumentId == command.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        string? oldKey = null;
        SignedDocument entity;
        if (existing is null)
        {
            var tenantId = currentUser.GetTenant() ?? dbContext.TenantInfo?.Identifier ?? string.Empty;
            entity = SignedDocument.Create(
                tenantId, command.DocumentType, command.DocumentId, storageKey, sha256,
                command.FileName, command.ContentType, command.Content.LongLength, uploaderId, uploader.Name);
            entity.CreatedBy = uploaderId.ToString();
            dbContext.SignedDocuments.Add(entity);
        }
        else
        {
            oldKey = existing.StorageKey;
            existing.Replace(storageKey, sha256, command.FileName, command.ContentType, command.Content.LongLength, uploaderId, uploader.Name);
            existing.LastModifiedBy = uploaderId.ToString();
            entity = existing;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The row didn't persist (e.g. a concurrent first-upload losing the unique-index race) —
            // don't leave the blob we just uploaded orphaned in storage.
            try { await storageService.RemoveAsync(storageKey, cancellationToken).ConfigureAwait(false); }
            catch (Exception) { /* best effort */ }
            throw;
        }

        // Best-effort cleanup of the replaced blob (after the row commit succeeded).
        if (oldKey is not null && !string.Equals(oldKey, storageKey, StringComparison.Ordinal))
        {
            try { await storageService.RemoveAsync(oldKey, cancellationToken).ConfigureAwait(false); }
            catch (Exception) { /* orphaned blob is harmless; the row is correct */ }
        }

        return ToDto(entity);
    }

    private async ValueTask EnsureDocumentSignedAsync(ProcurementDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case ProcurementDocumentType.PurchaseRequest:
                var pr = await dbContext.PurchaseRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Purchase request '{id}' not found.");
                // Signed at approval; remains a signed record through Completed.
                if (pr.Status is not (PurchaseRequestStatus.Approved or PurchaseRequestStatus.Completed))
                    throw new CustomException("A signed copy can only be uploaded once the purchase request is Approved.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                break;

            case ProcurementDocumentType.PurchaseOrder:
                var po = await dbContext.PurchaseOrders.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Purchase order '{id}' not found.");
                // Signed at issue; remains a signed record while deliveries are recorded (Partially/Fulfilled).
                if (po.Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyDelivered or PurchaseOrderStatus.Fulfilled))
                    throw new CustomException("A signed copy can only be uploaded once the purchase order is Issued.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                break;

            case ProcurementDocumentType.AbstractOfCanvass:
                var canvass = await dbContext.CanvassRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass request '{id}' not found.");
                if (canvass.Status != CanvassRequestStatus.Awarded)
                    throw new CustomException("A signed copy can only be uploaded for an Awarded canvass.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                break;

            case ProcurementDocumentType.RequestForQuotation:
                // The RFQ is the supplier's own wet-signed quotation document — it exists the moment the
                // quotation is recorded, so it may be attached at any non-cancelled canvass stage (unlike the
                // Abstract of Canvass, which summarises all quotations and is only signed once awarded).
                var quotation = await dbContext.CanvassQuotations.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass quotation '{id}' not found.");
                var parentCanvass = await dbContext.CanvassRequests.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == quotation.CanvassRequestId, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Canvass request '{quotation.CanvassRequestId}' not found.");
                if (parentCanvass.Status == CanvassRequestStatus.Cancelled)
                    throw new CustomException("A signed RFQ cannot be uploaded for a cancelled canvass.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                break;

            default:
                throw new CustomException($"Unsupported document type '{type}'.",
                    Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
        }
    }

    internal static SignedDocumentDto ToDto(SignedDocument d) => new(
        d.DocumentType, d.DocumentId, d.FileName, d.ContentType, d.FileSizeBytes, d.Sha256, d.UploadedByName, d.UploadedOnUtc);
}
