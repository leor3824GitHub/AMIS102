using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RecallAnnualProcurementPlan;

public sealed class RecallAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<RecallAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(RecallAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        app.Recall();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AppMapper.ToDto(app);
    }
}
