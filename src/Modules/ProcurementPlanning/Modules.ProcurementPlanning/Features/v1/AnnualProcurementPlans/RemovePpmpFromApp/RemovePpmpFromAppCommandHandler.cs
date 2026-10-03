using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.RemovePpmpFromApp;

public sealed class RemovePpmpFromAppCommandHandler(
    ProcurementPlanningDbContext dbContext) : ICommandHandler<RemovePpmpFromAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        RemovePpmpFromAppCommand command, CancellationToken cancellationToken)
    {
        var (_, ppmp, referencedByOtherApp) = await AppPpmpRelease
            .RemoveAsync(dbContext, command.AppId, command.PpmpId, cancellationToken)
            .ConfigureAwait(false);

        // Back to Approved so it can be consolidated again later — unless another APP version still holds it.
        if (ppmp.Status == PpmpStatus.Consolidated && !referencedByOtherApp)
            ppmp.UnmarkConsolidated();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, command.AppId, cancellationToken).ConfigureAwait(false);
    }
}
