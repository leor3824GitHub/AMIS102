using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.ReturnDisbursementVoucher;

public sealed class ReturnDisbursementVoucherCommandHandler(
    ILogger<ReturnDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<ReturnDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(ReturnDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Disbursement voucher '{command.Id}' not found.");

        try { dv.Return(command.Remarks); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Returned disbursement voucher {DvNumber}", dv.DvNumber);

        return Unit.Value;
    }
}
