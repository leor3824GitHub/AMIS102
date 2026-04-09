using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using FSH.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.ObligateBudgetUtilizationRecord;

public sealed class ObligateBudgetUtilizationRecordCommandHandler(
    ILogger<ObligateBudgetUtilizationRecordCommandHandler> logger,
    FinanceDbContext dbContext) : ICommandHandler<ObligateBudgetUtilizationRecordCommand>
{
    public async ValueTask<Unit> Handle(ObligateBudgetUtilizationRecordCommand command, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRecords
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Budget utilization record '{command.Id}' not found.");

        bur.Obligate();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Obligated budget utilization record {BurNumber}", bur.BurNumber);

        return Unit.Value;
    }
}
