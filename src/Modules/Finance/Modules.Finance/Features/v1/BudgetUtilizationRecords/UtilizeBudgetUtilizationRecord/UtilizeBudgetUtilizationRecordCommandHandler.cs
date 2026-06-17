using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.UtilizeBudgetUtilizationRecord;

public sealed class UtilizeBudgetUtilizationRecordCommandHandler(
    ILogger<UtilizeBudgetUtilizationRecordCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<UtilizeBudgetUtilizationRecordCommand>
{
    public async ValueTask<Unit> Handle(UtilizeBudgetUtilizationRecordCommand command, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRecords
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Budget utilization record '{command.Id}' not found.");

        try { bur.Utilize(command.DisbursementVoucherId, command.DisbursementVoucherNumber); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Utilized budget utilization record {BurNumber}", bur.BurNumber);

        return Unit.Value;
    }
}
