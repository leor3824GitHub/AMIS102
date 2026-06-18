using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.CancelDisbursementVoucher;

public sealed class CancelDisbursementVoucherCommandHandler(
    ILogger<CancelDisbursementVoucherCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<CancelDisbursementVoucherCommand>
{
    public async ValueTask<Unit> Handle(CancelDisbursementVoucherCommand command, CancellationToken cancellationToken)
    {
        var dv = await dbContext.DisbursementVouchers
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Disbursement voucher '{command.Id}' not found.");

        try { dv.Cancel(command.Remarks); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }

        // Cancelling the DV releases the budget it consumed: the linked BUR reverts from Utilized back
        // to Obligated (clearing the DV link) so a fresh DV can be raised against it.
        var bur = await dbContext.BudgetUtilizationRecords
            .FirstOrDefaultAsync(b => b.Id == dv.BudgetUtilizationRecordId, cancellationToken)
            .ConfigureAwait(false);
        if (bur is not null && bur.Status == BudgetUtilizationRecordStatus.Utilized)
            bur.Release();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cancelled disbursement voucher {DvNumber}; released BUR {BurNumber}",
            dv.DvNumber, dv.BurNumber);

        return Unit.Value;
    }
}

