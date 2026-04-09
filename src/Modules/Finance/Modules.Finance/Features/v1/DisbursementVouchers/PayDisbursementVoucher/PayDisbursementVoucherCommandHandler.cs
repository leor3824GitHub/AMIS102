using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using FSH.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.PayDisbursementVoucher;

public sealed class PayDisbursementVoucherCommandHandler(
    ILogger<PayDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<PayDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(PayDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{command.Id}' not found.");

        dv.Pay(command.PaidDate, command.Remarks);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked disbursement voucher {DvNumber} as paid", dv.DvNumber);

        return Unit.Value;
    }
}
