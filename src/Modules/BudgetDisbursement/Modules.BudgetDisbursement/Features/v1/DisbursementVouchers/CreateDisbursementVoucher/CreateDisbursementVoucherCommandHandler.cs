using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Persistence.Sequencing;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Features.v1.Shared;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public sealed class CreateDisbursementVoucherCommandHandler(
    ILogger<CreateDisbursementVoucherCommandHandler> logger,
    BudgetDisbursementDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser) : ICommandHandler<CreateDisbursementVoucherCommand, Guid>
{
    // A DV may only be raised against a PO that has at least been Issued to a supplier. Issued is
    // intentionally allowed (not just delivered) so cash-on-delivery / advance payments can be
    // vouchered before the goods arrive. Draft / PendingFundsAvailable / PendingApproval / Cancelled
    // are not payable.
    private static readonly PurchaseOrderStatus[] VoucherableStatuses =
        [PurchaseOrderStatus.Issued, PurchaseOrderStatus.PartiallyDelivered, PurchaseOrderStatus.Fulfilled];

    public async ValueTask<Guid> Handle(CreateDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        // BUR first, then DV: the voucher is raised against an obligated Budget Utilization Request. The
        // BUR is the single source of truth for the purchase order — we don't trust a client-supplied PO.
        var bur = await dbContext.BudgetUtilizationRequests
            .FirstOrDefaultAsync(b => b.Id == command.BudgetUtilizationRequestId, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Budget utilization record not found.", [], HttpStatusCode.NotFound);

        // Budget must be obligated before it can be disbursed. A BUR that already has a DV is Utilized,
        // so this status gate also enforces one DV per BUR.
        if (bur.Status != BudgetUtilizationRequestStatus.Obligated)
            throw new CustomException(
                "The budget utilization record must be obligated before a disbursement voucher can be raised against it.",
                [], HttpStatusCode.BadRequest);

        var po = await mediator.Send(new GetPurchaseOrderQuery(bur.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        if (!VoucherableStatuses.Contains(po.Status))
            throw new CustomException(
                "Purchase order must be issued before a disbursement voucher can be created.",
                [], HttpStatusCode.BadRequest);

        var hasExistingVoucher = await dbContext.DisbursementVouchers
            .AnyAsync(d => d.PurchaseOrderId == bur.PurchaseOrderId
                           && d.Status != DisbursementVoucherStatus.Cancelled, cancellationToken)
            .ConfigureAwait(false);
        if (hasExistingVoucher)
            throw new CustomException(
                "A disbursement voucher already exists for this purchase order.",
                [], HttpStatusCode.BadRequest);

        // Atomic number allocation: increment a per-year counter row (global — TenantId "") guarded by xmin,
        // retrying if a concurrent create advanced the counter or collided on the unique DvNumber index. The
        // BUR is utilized in the same transaction so it can never be left Obligated with an orphaned voucher.
        var year = command.DvDate.Year;
        for (var attempt = 0; attempt < SequenceAllocator.MaxAttempts; attempt++)
        {
            var serial = await SequenceAllocator.ReserveNextSerialAsync(
                dbContext, tenantId: string.Empty, sequenceKey: "DV", year, month: 0, cancellationToken,
                seedFactory: SeedFromIssuedAsync).ConfigureAwait(false);

            var dvNumber = $"DV-{year}-{serial:00000}";

            var dv = DisbursementVoucher.Create(
                currentUser.GetTenant() ?? string.Empty,
                dvNumber,
                bur.PurchaseOrderId,
                bur.PurchaseOrderNumber,
                bur.Id,
                bur.BurNumber,
                command.DvDate,
                command.FundCluster,
                command.Payee,
                command.TinNo,
                command.PayeeAddress,
                command.Particulars,
                command.Amount,
                command.ModeOfPayment,
                command.Remarks,
                command.Deductions?.Select(x => DvDeduction.Create(x.Name, x.Type, x.Value)));

            dv.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.DisbursementVouchers.Add(dv);

            // Link the voucher back to its BUR and mark the obligation Utilized.
            bur.Utilize(dv.Id, dv.DvNumber);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created disbursement voucher {DvNumber} for BUR {BurNumber} (PO {PoNumber})",
                    dvNumber, bur.BurNumber, bur.PurchaseOrderNumber);
                return dv.Id;
            }
            catch (DbUpdateException ex) when (SequenceAllocator.IsRetryableAllocationConflict(ex))
            {
                dbContext.ChangeTracker.Clear();

                // Re-fetch the BUR so it is tracked again for the next attempt (the clear detached it).
                bur = await dbContext.BudgetUtilizationRequests
                    .FirstOrDefaultAsync(b => b.Id == command.BudgetUtilizationRequestId, cancellationToken).ConfigureAwait(false)
                    ?? throw new CustomException("Budget utilization record not found.", [], HttpStatusCode.NotFound);
            }
        }

        throw new CustomException("Failed to allocate a unique DV number. Please try again.", [], HttpStatusCode.Conflict);

        // Seeds a first-created counter row from the highest DV number already issued this year — including
        // soft-deleted rows, which the global unique index still enforces — so allocation never re-issues one.
        async Task<int> SeedFromIssuedAsync()
        {
            var prefix = $"DV-{year}-";
            var issuedNumbers = await dbContext.DisbursementVouchers
                .IgnoreQueryFilters()
                .Where(d => d.DvNumber.StartsWith(prefix))
                .Select(d => d.DvNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return issuedNumbers.Count == 0 ? 0 : issuedNumbers.Max(BudgetDocumentNumber.ParseSerial);
        }
    }
}
