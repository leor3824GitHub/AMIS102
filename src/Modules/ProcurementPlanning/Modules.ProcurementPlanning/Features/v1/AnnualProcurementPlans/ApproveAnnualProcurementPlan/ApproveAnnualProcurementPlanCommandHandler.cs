using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ApproveAnnualProcurementPlan;

public sealed class ApproveAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<ApproveAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(ApproveAppCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found.");

        app.Approve(command.ApprovedById);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AppMapper.ToDto(app);
    }
}
