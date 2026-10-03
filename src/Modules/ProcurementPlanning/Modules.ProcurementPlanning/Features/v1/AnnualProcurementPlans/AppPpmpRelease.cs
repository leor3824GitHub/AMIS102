using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementPlanning.Data;
using AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans;

/// <summary>
/// Shared by Remove / Return PPMP-from-APP: takes a consolidated PPMP out of a Draft or Returned APP and
/// reports whether any other APP version still references it (in which case its status must stay Consolidated).
/// </summary>
internal static class AppPpmpRelease
{
    internal sealed record Result(AnnualProcurementPlan App, Ppmp Ppmp, bool ReferencedByOtherApp);

    internal static async Task<Result> RemoveAsync(
        ProcurementPlanningDbContext dbContext, Guid appId, Guid ppmpId, CancellationToken cancellationToken)
    {
        var app = await dbContext.AnnualProcurementPlans
            .Include(x => x.SourcePpmps)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == appId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"APP {appId} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var ppmp = await dbContext.Ppmps
            .FirstOrDefaultAsync(x => x.Id == ppmpId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new CustomException($"PPMP {ppmpId} not found.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        try
        {
            app.RemovePpmp(ppmpId);
        }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
        }

        // e.g. an Updated APP draft carries the Final PPMPs of the approved (now superseded) Final APP.
        var referencedByOtherApp = await dbContext.AppSourcePpmps
            .Where(s => s.PpmpId == ppmpId && s.AppId != appId)
            .Join(dbContext.AnnualProcurementPlans, s => s.AppId, a => a.Id, (_, a) => a.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        return new Result(app, ppmp, referencedByOtherApp);
    }
}
