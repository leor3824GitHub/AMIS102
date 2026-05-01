using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;

public sealed class PromoteToFinalAppCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<PromoteToFinalAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        PromoteToFinalAppCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.AnnualProcurementPlans
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found or is not the current version.");

        var userId = currentUser.GetUserId();
        var finalApp = original.PromoteToFinal(userId);
        original.Supersede();

        finalApp.CreatedBy = userId.ToString();
        dbContext.AnnualProcurementPlans.Add(finalApp);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, finalApp.Id, cancellationToken).ConfigureAwait(false);
    }
}
