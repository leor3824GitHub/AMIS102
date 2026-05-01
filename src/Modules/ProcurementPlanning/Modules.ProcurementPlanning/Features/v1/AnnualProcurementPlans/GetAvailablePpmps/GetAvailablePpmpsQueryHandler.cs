using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAvailablePpmps;

public sealed class GetAvailablePpmpsQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetAvailablePpmpsForAppQuery, IReadOnlyList<PpmpSummaryDto>>
{
    public async ValueTask<IReadOnlyList<PpmpSummaryDto>> Handle(
        GetAvailablePpmpsForAppQuery query, CancellationToken cancellationToken)
    {
        var app = query.AppId.HasValue
            ? await dbContext.AnnualProcurementPlans
                .AsNoTracking()
                .Where(x => x.Id == query.AppId.Value)
                .Select(x => new { x.Id, x.Phase })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;

        var appSourcePpmpIds = query.AppId.HasValue
            ? await dbContext.AppSourcePpmps
                .AsNoTracking()
                .Where(x => x.AppId == query.AppId.Value)
                .Select(x => x.PpmpId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

        return await dbContext.Ppmps
            .AsNoTracking()
            .Where(x => x.FiscalYear == query.FiscalYear
                     && x.IsCurrentVersion
                     && (!query.AppId.HasValue ||
                         (app!.Phase == AppPhase.Indicative && x.Phase == PpmpPhase.Indicative) ||
                         (app!.Phase == AppPhase.Final && x.Phase == PpmpPhase.Final) ||
                         (app!.Phase == AppPhase.Updated && (x.Phase == PpmpPhase.Final || x.Phase == PpmpPhase.Updated)))
                     && (x.Status == PpmpStatus.Approved
                         || (x.Status == PpmpStatus.Consolidated && appSourcePpmpIds.Contains(x.Id))))
            .OrderBy(x => x.OfficeCode).ThenBy(x => x.PpmpNumber)
            .Select(x => new PpmpSummaryDto(
                x.Id, x.PpmpNumber, x.FiscalYear, x.Phase,
                x.OfficeCode, x.EndUserUnit, x.Status,
                x.VersionNumber, x.IsCurrentVersion, x.VersionChainId,
                x.Items.Count,
                x.Items.Sum(i => i.EstimatedBudget),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
