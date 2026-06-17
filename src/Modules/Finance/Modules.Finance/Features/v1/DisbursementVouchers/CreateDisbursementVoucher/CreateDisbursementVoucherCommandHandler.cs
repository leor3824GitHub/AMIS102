using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using AMIS.Modules.Finance.Domain.DisbursementVouchers;
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

        var dvNumber = await GenerateDvNumberAsync(command.DvDate.Year, cancellationToken).ConfigureAwait(false);

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
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created disbursement voucher {DvNumber} for PO {PoNumber}", dvNumber, command.PurchaseOrderNumber);

        return dv.Id;
    }

    private async Task<string> GenerateDvNumberAsync(int year, CancellationToken ct)
    {
        var prefix = $"DV-{year}-";

        var lastNumber = await dbContext.DisbursementVouchers
            .IgnoreQueryFilters()
            .Where(x => x.DvNumber.StartsWith(prefix))
            .Select(x => x.DvNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
        {
            next = last + 1;
        }

        return $"{prefix}{next:00000}";
    }
}

