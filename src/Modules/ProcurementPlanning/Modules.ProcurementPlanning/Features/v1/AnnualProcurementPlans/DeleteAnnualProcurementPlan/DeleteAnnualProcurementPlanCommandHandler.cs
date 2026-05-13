using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.DeleteAnnualProcurementPlan;

public sealed class DeleteAnnualProcurementPlanCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteAnnualProcurementPlanCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAnnualProcurementPlanCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.SourcePpmps)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {command.Id} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        if (app.Status != AppStatus.Draft)
            throw new CustomException(
                "Only Draft APPs can be deleted. Published or Approved APPs must be recalled or amended.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var consolidatedPpmpIds = app.SourcePpmps.Select(i => i.PpmpId).Distinct().ToList();
        if (consolidatedPpmpIds.Count > 0)
        {
            var ppmps = await dbContext.Ppmps
                .Where(x => consolidatedPpmpIds.Contains(x.Id) && x.Status == PpmpStatus.Consolidated)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var ppmp in ppmps)
                ppmp.UnmarkConsolidated();
        }

        app.IsDeleted = true;
        app.DeletedOnUtc = DateTimeOffset.UtcNow;
        app.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
