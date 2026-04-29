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
        return await dbContext.Ppmps
            .AsNoTracking()
            .Where(x => x.FiscalYear == query.FiscalYear
                     && x.IsCurrentVersion
                     && (x.Status == PpmpStatus.Approved
                         || (x.Status == PpmpStatus.Consolidated && x.AppId == query.AppId)))
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
