using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.GetPpmpVersions;

public sealed class GetPpmpVersionsQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetPpmpVersionsQuery, IReadOnlyList<PpmpSummaryDto>>
{
    public async ValueTask<IReadOnlyList<PpmpSummaryDto>> Handle(
        GetPpmpVersionsQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.Ppmps
            .AsNoTracking()
            .Where(x => x.VersionChainId == query.VersionChainId)
            .OrderBy(x => x.VersionNumber)
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

