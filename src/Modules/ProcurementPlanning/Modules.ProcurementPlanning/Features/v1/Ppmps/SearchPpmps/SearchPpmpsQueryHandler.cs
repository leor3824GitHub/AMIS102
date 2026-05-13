using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.SearchPpmps;

public sealed class SearchPpmpsQueryHandler(
    ProcurementPlanningDbContext dbContext) : IQueryHandler<SearchPpmpsQuery, PagedResponse<PpmpSummaryDto>>
{
    public async ValueTask<PagedResponse<PpmpSummaryDto>> Handle(
        SearchPpmpsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.Ppmps
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.CurrentVersionOnly)
            q = q.Where(x => x.IsCurrentVersion);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.PpmpNumber.Contains(query.Keyword) || x.EndUserUnit.Contains(query.Keyword));

        if (query.FiscalYear.HasValue)
            q = q.Where(x => x.FiscalYear == query.FiscalYear.Value);

        if (!string.IsNullOrWhiteSpace(query.OfficeCode))
            q = q.Where(x => x.OfficeCode == query.OfficeCode);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.Phase.HasValue)
            q = q.Where(x => x.Phase == query.Phase.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new PpmpSummaryDto(
                x.Id, x.PpmpNumber, x.FiscalYear, x.Phase,
                x.OfficeCode, x.EndUserUnit, x.Status,
                x.VersionNumber, x.IsCurrentVersion, x.VersionChainId,
                x.Items.Count,
                x.Items.Sum(i => i.EstimatedBudget),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PpmpSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}

