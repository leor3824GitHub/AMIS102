using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PromoteToFinalApp;

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
            ?? throw new CustomException($"APP {command.Id} not found or is not the current version.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var userId = currentUser.GetUserId();
        var finalApp = original.PromoteToFinal(userId);
        original.Supersede();

        finalApp.CreatedBy = userId.ToString();
        dbContext.AnnualProcurementPlans.Add(finalApp);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, finalApp.Id, cancellationToken).ConfigureAwait(false);
    }
}

