using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.SearchAnnualProcurementPlans;

public sealed class SearchAnnualProcurementPlansQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<SearchAnnualProcurementPlansQuery, PagedResponse<AnnualProcurementPlanSummaryDto>>
{
    public async ValueTask<PagedResponse<AnnualProcurementPlanSummaryDto>> Handle(
        SearchAnnualProcurementPlansQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.AnnualProcurementPlans
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.CurrentVersionOnly)
            q = q.Where(x => x.IsCurrentVersion);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.AppNumber.Contains(query.Keyword));

        if (query.FiscalYear.HasValue)
            q = q.Where(x => x.FiscalYear == query.FiscalYear.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.Phase.HasValue)
            q = q.Where(x => x.Phase == query.Phase.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageApps = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
<<<<<<< HEAD
            .Select(x => new AnnualProcurementPlanSummaryDto(
                x.Id, x.AppNumber, x.FiscalYear, x.Phase, x.Status,
                x.VersionNumber, x.IsCurrentVersion, x.VersionChainId,
                x.Items.Count,
                x.Items.Sum(i => i.EstimatedBudget),
                x.CreatedOnUtc))
=======
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
>>>>>>> d63aec54a5aea0527fd07e545543a98aceae4138
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var appIds = pageApps.Select(x => x.Id).ToList();

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

        var items = pageApps
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

        return new PagedResponse<AnnualProcurementPlanSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
