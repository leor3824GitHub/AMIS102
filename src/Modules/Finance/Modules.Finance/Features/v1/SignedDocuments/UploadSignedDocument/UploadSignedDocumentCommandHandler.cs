using System.Net;
using System.Security.Cryptography;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Storage;
using AMIS.Framework.Storage;
using AMIS.Framework.Storage.Services;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.SignedDocuments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Finance.Features.v1.SignedDocuments.UploadSignedDocument;

public sealed class UploadSignedDocumentCommandHandler(
    FinanceDbContext dbContext,
    ICurrentUser currentUser,
    IStorageService storageService) : ICommandHandler<UploadSignedDocumentCommand, SignedDocumentDto>
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

        var uploaderId = currentUser.GetUserId();
        var uploaderName = currentUser.Name;

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
                command.FileName, command.ContentType, command.Content.LongLength, uploaderId, uploaderName);
            entity.CreatedBy = uploaderId.ToString();
            dbContext.SignedDocuments.Add(entity);
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

    private async ValueTask EnsureDocumentSignedAsync(FinanceDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case FinanceDocumentType.DisbursementVoucher:
                var dv = await dbContext.DisbursementVouchers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Disbursement voucher '{id}' not found.");
                // Signed once approved; remains a signed record through payment.
                if (dv.Status is not (DisbursementVoucherStatus.Approved or DisbursementVoucherStatus.Paid))
                    throw new CustomException("A signed copy can only be uploaded once the disbursement voucher is Approved.",
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
