using FSH.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using FSH.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Finance.Features.v1.BudgetUtilizationRecords.SearchBudgetUtilizationRecords;

public sealed class SearchBudgetUtilizationRecordsQueryHandler(
    FinanceDbContext dbContext) : IQueryHandler<SearchBudgetUtilizationRecordsQuery, BudgetUtilizationRecordSearchResult>
{
    public async ValueTask<BudgetUtilizationRecordSearchResult> Handle(SearchBudgetUtilizationRecordsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.BudgetUtilizationRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.BurNumber.ToLower().Contains(kw) ||
                x.PurchaseOrderNumber.ToLower().Contains(kw) ||
                x.Particulars.ToLower().Contains(kw) ||
                x.UacsObjectCode.ToLower().Contains(kw));
        }

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.PurchaseOrderId.HasValue)
            q = q.Where(x => x.PurchaseOrderId == query.PurchaseOrderId.Value);

        if (!string.IsNullOrWhiteSpace(query.AllotmentClass))
            q = q.Where(x => x.AllotmentClass == query.AllotmentClass);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.BurDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new BudgetUtilizationRecordListItemDto(
                x.Id,
                x.BurNumber,
                x.BurDate,
                x.PurchaseOrderNumber,
                x.AllotmentClass,
                x.Amount,
                x.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BudgetUtilizationRecordSearchResult(items, totalCount, query.PageNumber, query.PageSize);
    }
}
