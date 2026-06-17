using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using AMIS.Modules.Finance.Features.v1.Shared;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public sealed class CreateDisbursementVoucherCommandHandler(
    ILogger<CreateDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext,
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
        var po = await mediator.Send(new GetPurchaseOrderQuery(command.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        if (!VoucherableStatuses.Contains(po.Status))
            throw new CustomException(
                "Purchase order must be issued before a disbursement voucher can be created.",
                [], HttpStatusCode.BadRequest);

        var hasExistingVoucher = await dbContext.DisbursementVouchers
            .AnyAsync(d => d.PurchaseOrderId == command.PurchaseOrderId
                           && d.Status != DisbursementVoucherStatus.Cancelled, cancellationToken)
            .ConfigureAwait(false);
        if (hasExistingVoucher)
            throw new CustomException(
                "A disbursement voucher already exists for this purchase order.",
                [], HttpStatusCode.BadRequest);

        // Atomic number allocation: increment a per-year counter row guarded by xmin, retrying if a
        // concurrent create advanced the counter or collided on the unique DvNumber index.
        for (var attempt = 0; attempt < FinanceSequenceAllocation.MaxAttempts; attempt++)
        {
            var year = command.DvDate.Year;

            var sequence = await dbContext.DvNumberSequences
                .FirstOrDefaultAsync(x => x.Year == year, cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = DvNumberSequence.Create(year);
                dbContext.DvNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var dvNumber = $"DV-{year}-{serial:00000}";

            var dv = DisbursementVoucher.Create(
                dvNumber,
                command.PurchaseOrderId,
                command.PurchaseOrderNumber,
                command.DvDate,
                command.FundCluster,
                command.Payee,
                command.TinNo,
                command.PayeeAddress,
                command.Particulars,
                command.Amount,
                command.ModeOfPayment,
                command.Remarks);

            dv.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.DisbursementVouchers.Add(dv);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Created disbursement voucher {DvNumber} for PO {PoNumber}", dvNumber, command.PurchaseOrderNumber);
                return dv.Id;
            }
            catch (DbUpdateException ex) when (FinanceSequenceAllocation.IsRetryableAllocationConflict(ex))
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique DV number. Please try again.", [], HttpStatusCode.Conflict);
    }
}

