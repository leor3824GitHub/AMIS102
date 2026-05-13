using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ApproveAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(ApproveAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        app.Approve(currentUser.GetUserId());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}
