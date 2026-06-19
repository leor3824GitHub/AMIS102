using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Finance.Features.v1.BudgetUtilizationRecords.GetStatusCounts;

public sealed class GetBudgetUtilizationRecordStatusCountsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetBudgetUtilizationRecordStatusCountsQuery, IReadOnlyList<BudgetUtilizationRecordStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<BudgetUtilizationRecordStatusCountDto>> Handle(
        GetBudgetUtilizationRecordStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.BudgetUtilizationRecords.AsNoTracking();

        // Mirror SearchBudgetUtilizationRecordsQueryHandler's keyword filter so the badge counts
        // match exactly what the list returns for the same search.
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.BurNumber.ToLower().Contains(kw) ||
                x.PurchaseOrderNumber.ToLower().Contains(kw) ||
                x.Particulars.ToLower().Contains(kw) ||
                x.UacsObjectCode.ToLower().Contains(kw));
        }

        return await q
            .GroupBy(x => x.Status)
            .Select(g => new BudgetUtilizationRecordStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
