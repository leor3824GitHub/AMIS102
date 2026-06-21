using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.PayDisbursementVoucher;

public sealed class PayDisbursementVoucherCommandHandler(
    ILogger<PayDisbursementVoucherCommandHandler> logger,
    BudgetDisbursementDbContext dbContext) : ICommandHandler<PayDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(PayDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Disbursement voucher '{command.Id}' not found.");

        try { dv.Pay(command.PaidDate, command.Remarks); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked disbursement voucher {DvNumber} as paid", dv.DvNumber);

        return Unit.Value;
    }
}

