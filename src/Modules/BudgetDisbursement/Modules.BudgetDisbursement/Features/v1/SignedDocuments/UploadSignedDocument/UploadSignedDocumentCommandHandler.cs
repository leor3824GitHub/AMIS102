using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Domain;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Storage.Services;
using AMIS.Framework.Storage.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.SignedDocuments.UploadSignedDocument;

public sealed class UploadSignedDocumentCommandHandler(
    BudgetDisbursementDbContext dbContext,
    ICurrentUser currentUser,
    IStorageService storageService) : ICommandHandler<UploadSignedDocumentCommand, SignedDocumentDto>
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

        var copy = await SignedCopyStore.BuildAsync(
            storageService, tenantId, command.DocumentType.ToString(), command.Content,
            command.FileName, currentUser.Name, cancellationToken).ConfigureAwait(false);

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

    private async ValueTask<ISignedCopyHolder> LoadSignableDocumentAsync(BudgetDisbursementDocumentType type, Guid id, CancellationToken ct)
    {
        switch (type)
        {
            case BudgetDisbursementDocumentType.DisbursementVoucher:
                var dv = await dbContext.DisbursementVouchers
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Disbursement voucher '{id}' not found.");
                // Signed once approved; remains a signed record through payment.
                if (dv.Status is not (DisbursementVoucherStatus.Approved or DisbursementVoucherStatus.Paid))
                    throw new CustomException("A signed copy can only be uploaded once the disbursement voucher is Approved.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return dv;

            case BudgetDisbursementDocumentType.BudgetUtilizationRequest:
                var bur = await dbContext.BudgetUtilizationRequests
                    .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false)
                    ?? throw new NotFoundException($"Budget utilization record '{id}' not found.");
                // Signed once the budget officer obligates it; remains a signed record once utilized by a DV.
                if (bur.Status is not (BudgetUtilizationRequestStatus.Obligated or BudgetUtilizationRequestStatus.Utilized))
                    throw new CustomException("A signed copy can only be uploaded once the budget utilization record is Obligated.",
                        Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
                return bur;

            default:
                throw new CustomException($"Unsupported document type '{type}'.",
                    Enumerable.Empty<string>(), HttpStatusCode.BadRequest);
        }
    }

    internal static SignedDocumentDto ToDto(BudgetDisbursementDocumentType type, Guid documentId, SignedCopy copy) => new(
        type, documentId, copy.FileName, "application/pdf", copy.FileSizeBytes, copy.Sha256, copy.UploadedByName, copy.UploadedOnUtc);
}
