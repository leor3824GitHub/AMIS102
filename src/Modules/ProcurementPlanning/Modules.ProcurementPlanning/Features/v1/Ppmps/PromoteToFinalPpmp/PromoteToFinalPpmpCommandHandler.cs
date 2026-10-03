using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.PromoteToFinalPpmp;

public sealed class PromoteToFinalPpmpCommandHandler(
    ProcurementPlanningDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<PromoteToFinalPpmpCommand, PpmpDto>
{
    public async ValueTask<PpmpDto> Handle(PromoteToFinalPpmpCommand command, CancellationToken cancellationToken)
    {
        var original = await dbContext.Ppmps
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == command.Id && x.IsCurrentVersion, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {command.Id} not found or is not the current version.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // A Consolidated PPMP is referenced by an Indicative APP; only promote once that APP is approved,
        // so an in-progress APP never ends up pointing at a superseded PPMP.
        if (original.Status == PpmpStatus.Consolidated)
        {
            var pendingApp = await dbContext.AppSourcePpmps
                .Where(s => s.PpmpId == original.Id)
                .Join(dbContext.AnnualProcurementPlans, s => s.AppId, a => a.Id, (_, a) => a)
                .Where(a => a.IsCurrentVersion && a.Status != AppStatus.Approved && a.Status != AppStatus.Superseded)
                .Select(a => a.AppNumber)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (pendingApp is not null)
                throw new CustomException(
                    $"Indicative APP {pendingApp} must be approved before its PPMPs can be promoted to Final.",
                    Enumerable.Empty<string>(),
                    HttpStatusCode.Conflict);
        }

        var userId = currentUser.GetUserId();
        Ppmp finalPpmp;
        try
        {
            finalPpmp = original.PromoteToFinal(userId);
            original.Supersede();
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }

        finalPpmp.CreatedBy = userId.ToString();
        dbContext.Ppmps.Add(finalPpmp);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PpmpMapper.ToDto(finalPpmp);
    }
}

