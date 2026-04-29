using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.AmendAnnualProcurementPlan;

public sealed class CreateUpdateAppCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateUpdateAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        CreateUpdateAppCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.AnnualProcurementPlans
            .Include(x => x.LineReferences)
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.Id} not found or is not the current version.");

        var userId = currentUser.GetUserId();
        var update = original.CreateUpdate(command.UpdateReason, userId);

        original.Supersede();

        update.CreatedBy = userId.ToString();
        dbContext.AnnualProcurementPlans.Add(update);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, update.Id, cancellationToken).ConfigureAwait(false);
    }
}
