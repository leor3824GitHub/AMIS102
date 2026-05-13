using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RecallAnnualProcurementPlan;

public sealed class RecallAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<RecallAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(RecallAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        app.Recall();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}

