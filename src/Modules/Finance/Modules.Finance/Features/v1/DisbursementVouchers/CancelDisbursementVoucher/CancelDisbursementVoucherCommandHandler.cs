using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;
using FSH.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public sealed class CancelDisbursementVoucherCommandHandler(
    ILogger<CancelDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<CancelDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(CancelDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{command.Id}' not found.");

        dv.Cancel(command.Remarks);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cancelled disbursement voucher {DvNumber}", dv.DvNumber);

        return Unit.Value;
    }
}
