using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ConsolidatePpmps;

public sealed class ConsolidatePpmpsCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<ConsolidatePpmpsCommand, AnnualProcurementPlanDto>
{
    public async ValueTask<AnnualProcurementPlanDto> Handle(
        ConsolidatePpmpsCommand command, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.AppId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"APP {command.AppId} not found.");

        var ppmps = await dbContext.Ppmps
            .Include(x => x.Items)
            .Where(x => command.PpmpIds.Contains(x.Id) && x.Status == Contracts.v1.Ppmps.PpmpStatus.Approved)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (ppmps.Count == 0)
            throw new InvalidOperationException("No Approved PPMPs found for the provided IDs.");

        var userId = currentUser.GetUserId();
        app.ConsolidatePpmps(ppmps, userId);

        // Mark consolidated PPMPs
        foreach (var ppmp in ppmps)
            ppmp.MarkConsolidated(app.Id);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AppMapper.ToDto(app);
    }
}
