using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.CreateUpdateApp;

public sealed class CreateUpdateAppCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateUpdateAppCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        CreateUpdateAppCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.AnnualProcurementPlans
            .Include(x => x.SourcePpmps)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.Id} not found or is not the current version.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var userId = currentUser.GetUserId();
        var update = original.CreateUpdate(command.UpdateReason, userId);

        original.Supersede();

        update.CreatedBy = userId.ToString();
        dbContext.AnnualProcurementPlans.Add(update);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AppReadProjection.BuildDtoAsync(dbContext, update.Id, cancellationToken).ConfigureAwait(false);
    }
}

