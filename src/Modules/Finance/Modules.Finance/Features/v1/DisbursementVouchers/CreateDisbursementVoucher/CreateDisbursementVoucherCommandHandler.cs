using FSH.Framework.Core.Context;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using FSH.Modules.Finance.Data;
using FSH.Modules.Finance.Domain.DisbursementVouchers;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.CreateDisbursementVoucher;

public sealed class CreateDisbursementVoucherCommandHandler(
    ILogger<CreateDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateDisbursementVoucherCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
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
