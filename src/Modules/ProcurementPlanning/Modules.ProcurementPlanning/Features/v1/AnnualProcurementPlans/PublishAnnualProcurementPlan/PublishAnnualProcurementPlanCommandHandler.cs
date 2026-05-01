using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.PublishAnnualProcurementPlan;

public sealed class PublishAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    FSH.Framework.Core.Context.ICurrentUser currentUser) : ICommandHandler<PublishAnnualProcurementPlanCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        PublishAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        app.Publish();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, app.Id, cancellationToken).ConfigureAwait(false);
    }
}