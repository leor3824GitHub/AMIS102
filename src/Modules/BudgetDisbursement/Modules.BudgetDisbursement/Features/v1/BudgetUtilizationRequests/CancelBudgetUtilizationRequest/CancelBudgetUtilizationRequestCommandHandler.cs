using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.CancelBudgetUtilizationRequest;

public sealed class CancelBudgetUtilizationRequestCommandHandler(
    ILogger<CancelBudgetUtilizationRequestCommandHandler> logger,
    BudgetDisbursementDbContext dbContext) : ICommandHandler<CancelBudgetUtilizationRequestCommand>
{
    public async ValueTask<Unit> Handle(CancelBudgetUtilizationRequestCommand command, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Budget utilization record '{command.Id}' not found.");

        try { bur.Cancel(command.Remarks); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Cancelled budget utilization record {BurNumber}", bur.BurNumber);

        return Unit.Value;
    }
}

