using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Domain;
using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.SignedDocuments.UploadSignedDocument;

public sealed class UploadSignedDocumentCommandHandler(
    AssetRegisterDbContext db,
    ICurrentUser currentUser,
    IStorageService storageService) : ICommandHandler<UploadSignedDocumentCommand, SignedDocumentDto>
{
    public async ValueTask<SignedDocumentDto> Handle(UploadSignedDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Content is null || command.Content.Length == 0)
            throw new InvalidOperationException("Uploaded file is empty.");

        // The signed copy may only be attached once the document has reached its terminal signed state.
        // Loaded tracked so the inlined SignedCopy is persisted with the aggregate on SaveChanges.
        var holder = await LoadSignableDocumentAsync(command.DocumentType, command.DocumentId, cancellationToken).ConfigureAwait(false);

        var tenantId = currentUser.GetTenant() ?? db.TenantInfo?.Identifier ?? string.Empty;
        var oldKey = holder.SignedCopy?.StorageKey;

        var copy = await SignedCopyStore.BuildAsync(
            storageService, tenantId, command.DocumentType.ToString(), command.Content,
            command.FileName, currentUser.Name, cancellationToken).ConfigureAwait(false);

        holder.SetSignedCopy(copy);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<ISignedCopyHolder> LoadSignableDocumentAsync(AssetRegisterDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case AssetRegisterDocumentType.ReturnedPropertyReceipt:
                var receipt = await db.ReturnedPropertyReceipts
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Returned-property receipt '{id}' not found.");
                // Signed on acceptance — the official receipt number is assigned then.
                if (receipt.Status != ReturnedPropertyReceiptStatus.Accepted)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the return has been Accepted.");
                return receipt;

            case AssetRegisterDocumentType.PropertyAccountability:
                var accountability = await db.PropertyAccountabilities
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Property accountability '{id}' not found.");
                // The ICS/PAR is wet-signed when the accountable employee accepts it; AcceptedOn is stamped then.
                if (!accountability.AcceptedOn.HasValue)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the ICS/PAR has been accepted.");
                return accountability;

            case AssetRegisterDocumentType.IssuanceReport:
                // PPEIR/SMIR is created atomically (creating it issues the assets) — a document of record on existence.
                return await db.PropertyIssuanceReports
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Issuance report '{id}' not found.");

            case AssetRegisterDocumentType.ReceivingReport:
                // PPERR/SMRR is created atomically — a document of record on existence.
                return await db.ReceivingReports
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Receiving report '{id}' not found.");

            case AssetRegisterDocumentType.UnserviceableReport:
                var unserviceable = await db.UnserviceablePropertyReports
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Unserviceable-property report '{id}' not found.");
                // Fully signed once the inspection/disposal is recorded (or the report is closed).
                if (unserviceable.Status is not (UnserviceableReportStatus.DisposalRecorded or UnserviceableReportStatus.Closed))
                    throw new InvalidOperationException("A signed copy can only be uploaded once disposal has been recorded.");
                return unserviceable;

            case AssetRegisterDocumentType.IncidentReport:
                var incident = await db.PropertyIncidentReports
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Incident report '{id}' not found.");
                // The notarised RLSDDSP of record exists once the incident is resolved/closed.
                if (incident.Status is not (PropertyIncidentStatus.Resolved or PropertyIncidentStatus.Closed))
                    throw new InvalidOperationException("A signed copy can only be uploaded once the incident is Resolved or Closed.");
                return incident;

            case AssetRegisterDocumentType.PhysicalCountReport:
                var session = await db.PhysicalCountSessions
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Physical-count session '{id}' not found.");
                // The RPCPPE/RPCSE is signed off when the count is closed.
                if (session.Status != PhysicalCountStatus.Closed)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the physical count is Closed.");
                return session;

            default:
                throw new InvalidOperationException($"Unsupported document type '{type}'.");
        }
    }

    internal static SignedDocumentDto ToDto(AssetRegisterDocumentType type, Guid documentId, SignedCopy copy) => new(
        type, documentId, copy.FileName, "application/pdf", copy.FileSizeBytes, copy.Sha256, copy.UploadedByName, copy.UploadedOnUtc);
}
