using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.UpdateDisbursementVoucher;

public sealed class UpdateDisbursementVoucherCommandHandler(
    ILogger<UpdateDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext,
    IMediator mediator) : ICommandHandler<UpdateDisbursementVoucherCommand>
{
    // Mirrors CreateDisbursementVoucherCommandHandler: a DV may only target a PO that has at least
    // been Issued (COD/advance payments are allowed before delivery).
    private static readonly PurchaseOrderStatus[] VoucherableStatuses =
        [PurchaseOrderStatus.Issued, PurchaseOrderStatus.PartiallyDelivered, PurchaseOrderStatus.Fulfilled];

    public async ValueTask<Unit> Handle(UpdateDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Disbursement voucher not found.", [], HttpStatusCode.NotFound);

        if (dv.Status != DisbursementVoucherStatus.Draft)
            throw new CustomException("Only Draft disbursement vouchers can be edited.", [], HttpStatusCode.BadRequest);

        var po = await mediator.Send(new GetPurchaseOrderQuery(command.PurchaseOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Purchase order not found.", [], HttpStatusCode.NotFound);

        if (!VoucherableStatuses.Contains(po.Status))
            throw new CustomException(
                "Purchase order must be issued before a disbursement voucher can be created.",
                [], HttpStatusCode.BadRequest);

        // One non-cancelled DV per PO — excluding this voucher so re-saving the same PO is allowed.
        var hasOtherVoucher = await dbContext.DisbursementVouchers
            .AnyAsync(d => d.PurchaseOrderId == command.PurchaseOrderId
                           && d.Id != command.Id
                           && d.Status != DisbursementVoucherStatus.Cancelled, cancellationToken)
            .ConfigureAwait(false);
        if (hasOtherVoucher)
            throw new CustomException(
                "A disbursement voucher already exists for this purchase order.",
                [], HttpStatusCode.BadRequest);

        dv.Update(
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
            command.Remarks,
            command.Deductions?.Select(x => DvDeduction.Create(x.Name, x.Type, x.Value)));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated disbursement voucher {DvNumber}", dv.DvNumber);

        return Unit.Value;
    }
}
