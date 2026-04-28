using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.GetAppVersions;

public sealed class GetAppVersionsQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<GetAppVersionsQuery, IReadOnlyList<AnnualProcurementPlanSummaryDto>>
{
    public async ValueTask<IReadOnlyList<AnnualProcurementPlanSummaryDto>> Handle(
        GetAppVersionsQuery query, CancellationToken cancellationToken)
    {
        var versions = await dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .Where(x => x.VersionChainId == query.VersionChainId)
            .OrderBy(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.AppNumber,
                x.FiscalYear,
                x.RevisionType,
                x.Status,
                x.VersionNumber,
                x.IsCurrentVersion,
                x.VersionChainId,
                x.CreatedOnUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appIds = versions.Select(x => x.Id).ToList();

        var aggregates = await (
                from line in dbContext.AppLineReferences.AsNoTracking()
                join ppmpItem in dbContext.PpmpItems.AsNoTracking() on line.SourcePpmpItemId equals ppmpItem.Id
                where appIds.Contains(line.AppId)
                group ppmpItem by line.AppId
                into g
                select new
                {
                    AppId = g.Key,
                    ItemCount = g.Count(),
                    TotalEstimatedBudget = g.Sum(x => x.EstimatedBudget)
                })
            .ToDictionaryAsync(x => x.AppId, cancellationToken)
            .ConfigureAwait(false);

        return versions
            .Select(x =>
            {
                var hasAgg = aggregates.TryGetValue(x.Id, out var agg);
                return new AnnualProcurementPlanSummaryDto(
                    x.Id,
                    x.AppNumber,
                    x.FiscalYear,
                    x.RevisionType,
                    x.Status,
                    x.VersionNumber,
                    x.IsCurrentVersion,
                    x.VersionChainId,
                    hasAgg ? agg!.ItemCount : 0,
                    hasAgg ? agg!.TotalEstimatedBudget : 0,
                    x.CreatedOnUtc);
            })
            .ToList();
    }
}
