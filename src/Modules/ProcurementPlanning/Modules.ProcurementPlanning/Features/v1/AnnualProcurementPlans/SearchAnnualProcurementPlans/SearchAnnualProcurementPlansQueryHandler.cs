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
        var q = dbContext.AnnualProcurementPlans.AsNoTracking();

        if (query.CurrentVersionOnly)
            q = q.Where(x => x.IsCurrentVersion);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.AppNumber.Contains(query.Keyword));

        if (query.FiscalYear.HasValue)
            q = q.Where(x => x.FiscalYear == query.FiscalYear.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.RevisionType.HasValue)
            q = q.Where(x => x.RevisionType == query.RevisionType.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AnnualProcurementPlanSummaryDto(
                x.Id, x.AppNumber, x.FiscalYear, x.RevisionType, x.Status,
                x.VersionNumber, x.IsCurrentVersion, x.VersionChainId,
                x.Items.Count,
                x.Items.Sum(i => i.EstimatedBudget),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<AnnualProcurementPlanSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
