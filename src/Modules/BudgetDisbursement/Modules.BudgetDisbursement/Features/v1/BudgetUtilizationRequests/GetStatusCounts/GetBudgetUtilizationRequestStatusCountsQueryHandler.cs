using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.GetStatusCounts;

public sealed class GetBudgetUtilizationRequestStatusCountsQueryHandler(BudgetDisbursementDbContext dbContext)
    : IQueryHandler<GetBudgetUtilizationRequestStatusCountsQuery, IReadOnlyList<BudgetUtilizationRequestStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<BudgetUtilizationRequestStatusCountDto>> Handle(
        GetBudgetUtilizationRequestStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.BudgetUtilizationRequests.AsNoTracking();

        // Mirror SearchBudgetUtilizationRequestsQueryHandler's keyword filter so the badge counts
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
            .Select(g => new BudgetUtilizationRequestStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
