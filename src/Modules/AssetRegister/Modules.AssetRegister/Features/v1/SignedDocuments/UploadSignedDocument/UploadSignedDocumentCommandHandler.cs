using System.Security.Cryptography;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.SignedDocuments;
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

        var uploaderId = currentUser.GetUserId();
        var uploaderName = currentUser.Name;

        var existing = await db.SignedDocuments
            .FirstOrDefaultAsync(x => x.DocumentType == command.DocumentType && x.DocumentId == command.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        string? oldKey = null;
        SignedDocument entity;
        if (existing is null)
        {
            var tenantId = currentUser.GetTenant() ?? db.TenantInfo?.Identifier ?? string.Empty;
            entity = SignedDocument.Create(
                tenantId, command.DocumentType, command.DocumentId, storageKey, sha256,
                command.FileName, command.ContentType, command.Content.LongLength, uploaderId, uploaderName);
            entity.CreatedBy = uploaderId.ToString();
            db.SignedDocuments.Add(entity);
        }
        else
        {
            oldKey = existing.StorageKey;
            existing.Replace(storageKey, sha256, command.FileName, command.ContentType, command.Content.LongLength, uploaderId, uploaderName);
            existing.LastModifiedBy = uploaderId.ToString();
            entity = existing;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    private async ValueTask EnsureDocumentSignedAsync(AssetRegisterDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case AssetRegisterDocumentType.ReturnedPropertyReceipt:
                var receipt = await db.ReturnedPropertyReceipts.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Returned-property receipt '{id}' not found.");
                // Signed on acceptance — the official receipt number is assigned then.
                if (receipt.Status != ReturnedPropertyReceiptStatus.Accepted)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the return has been Accepted.");
                break;

            case AssetRegisterDocumentType.PropertyAccountability:
                var accountability = await db.PropertyAccountabilities.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Property accountability '{id}' not found.");
                // The ICS/PAR is wet-signed when the accountable employee accepts it; AcceptedOn is stamped then.
                if (!accountability.AcceptedOn.HasValue)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the ICS/PAR has been accepted.");
                break;

            case AssetRegisterDocumentType.IssuanceReport:
                // PPEIR/SMIR is created atomically (creating it issues the assets) — a document of record on existence.
                _ = await db.PropertyIssuanceReports.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Issuance report '{id}' not found.");
                break;

            case AssetRegisterDocumentType.ReceivingReport:
                // PPERR/SMRR is created atomically — a document of record on existence.
                _ = await db.ReceivingReports.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Receiving report '{id}' not found.");
                break;

            case AssetRegisterDocumentType.UnserviceableReport:
                var unserviceable = await db.UnserviceablePropertyReports.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Unserviceable-property report '{id}' not found.");
                // Fully signed once the inspection/disposal is recorded (or the report is closed).
                if (unserviceable.Status is not (UnserviceableReportStatus.DisposalRecorded or UnserviceableReportStatus.Closed))
                    throw new InvalidOperationException("A signed copy can only be uploaded once disposal has been recorded.");
                break;

            case AssetRegisterDocumentType.IncidentReport:
                var incident = await db.PropertyIncidentReports.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Incident report '{id}' not found.");
                // The notarised RLSDDSP of record exists once the incident is resolved/closed.
                if (incident.Status is not (PropertyIncidentStatus.Resolved or PropertyIncidentStatus.Closed))
                    throw new InvalidOperationException("A signed copy can only be uploaded once the incident is Resolved or Closed.");
                break;

            case AssetRegisterDocumentType.PhysicalCountReport:
                var session = await db.PhysicalCountSessions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException($"Physical-count session '{id}' not found.");
                // The RPCPPE/RPCSE is signed off when the count is closed.
                if (session.Status != PhysicalCountStatus.Closed)
                    throw new InvalidOperationException("A signed copy can only be uploaded once the physical count is Closed.");
                break;

            default:
                throw new InvalidOperationException($"Unsupported document type '{type}'.");
        }
    }

    internal static SignedDocumentDto ToDto(SignedDocument d) => new(
        d.DocumentType, d.DocumentId, d.FileName, d.ContentType, d.FileSizeBytes, d.Sha256, d.UploadedByName, d.UploadedOnUtc);
}
