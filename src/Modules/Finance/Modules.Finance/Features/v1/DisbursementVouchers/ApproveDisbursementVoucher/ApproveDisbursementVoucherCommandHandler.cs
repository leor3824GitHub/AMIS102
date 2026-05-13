using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.ApproveDisbursementVoucher;

public sealed class ApproveDisbursementVoucherCommandHandler(
    ILogger<ApproveDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<ApproveDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(ApproveDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Disbursement voucher '{command.Id}' not found.");

        dv.Approve();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Approved disbursement voucher {DvNumber}", dv.DvNumber);

        return Unit.Value;
    }
}

