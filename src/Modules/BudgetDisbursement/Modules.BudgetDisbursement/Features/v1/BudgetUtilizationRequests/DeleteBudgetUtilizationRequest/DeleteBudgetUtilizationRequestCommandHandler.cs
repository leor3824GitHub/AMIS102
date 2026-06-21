using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.DeleteBudgetUtilizationRequest;

public sealed class DeleteBudgetUtilizationRequestCommandHandler(
    ILogger<DeleteBudgetUtilizationRequestCommandHandler> logger,
    BudgetDisbursementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteBudgetUtilizationRequestCommand>
{
    public async ValueTask<Unit> Handle(DeleteBudgetUtilizationRequestCommand command, CancellationToken cancellationToken)
    {
        var bur = await dbContext.BudgetUtilizationRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Budget utilization request '{command.Id}' not found.");

        if (bur.Status != BudgetUtilizationRequestStatus.Draft)
            throw new CustomException("Only Draft BURs can be deleted.", [], HttpStatusCode.BadRequest);

        bur.IsDeleted = true;
        bur.DeletedOnUtc = DateTimeOffset.UtcNow;
        bur.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deleted budget utilization request {BurNumber}", bur.BurNumber);

        return Unit.Value;
    }
}
