using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ReturnPpmpFromApp;

public sealed class ReturnPpmpFromAppCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ReturnPpmpFromAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        ReturnPpmpFromAppCommand command, CancellationToken cancellationToken)
    {
        var (_, ppmp, referencedByOtherApp) = await AppPpmpRelease
            .RemoveAsync(dbContext, command.AppId, command.PpmpId, cancellationToken)
            .ConfigureAwait(false);

        // A PPMP that is part of another (approved/superseded) APP version is historical record — it must be
        // revised through Create Update, not reopened in place.
        if (referencedByOtherApp)
            throw new CustomException(
                $"{ppmp.PpmpNumber} is part of another APP version and cannot be reopened. " +
                "Remove it here instead and ask the end-user to use Create Update on the PPMP.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        try
        {
            ppmp.ReturnFromConsolidation(command.ReturnReason.Trim(), currentUser.GetUserId());
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, command.AppId, cancellationToken).ConfigureAwait(false);
    }
}
