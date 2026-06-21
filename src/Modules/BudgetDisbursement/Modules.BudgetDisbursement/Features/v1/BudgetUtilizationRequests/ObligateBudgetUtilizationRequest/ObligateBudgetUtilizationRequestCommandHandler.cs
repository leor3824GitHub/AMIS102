using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.ObligateBudgetUtilizationRequest;

public sealed class ObligateBudgetUtilizationRequestCommandHandler(
    ILogger<ObligateBudgetUtilizationRequestCommandHandler> logger,
    BudgetDisbursementDbContext dbContext) : ICommandHandler<ObligateBudgetUtilizationRequestCommand>
{
    public async ValueTask<Unit> Handle(ObligateBudgetUtilizationRequestCommand command, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Budget utilization record '{command.Id}' not found.");

        try { bur.Obligate(); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Obligated budget utilization record {BurNumber}", bur.BurNumber);

        return Unit.Value;
    }
}

