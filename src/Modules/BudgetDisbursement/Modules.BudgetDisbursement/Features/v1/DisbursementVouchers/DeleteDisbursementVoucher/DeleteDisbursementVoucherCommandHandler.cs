using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.DeleteDisbursementVoucher;

public sealed class DeleteDisbursementVoucherCommandHandler(
    ILogger<DeleteDisbursementVoucherCommandHandler> logger,
    BudgetDisbursementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(DeleteDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Disbursement voucher '{command.Id}' not found.");

        if (dv.Status != DisbursementVoucherStatus.Draft)
            throw new CustomException("Only Draft disbursement vouchers can be deleted.", [], HttpStatusCode.BadRequest);

        dv.SoftDelete(currentUser.GetUserId().ToString());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deleted disbursement voucher {DvNumber}", dv.DvNumber);

        return Unit.Value;
    }
}
