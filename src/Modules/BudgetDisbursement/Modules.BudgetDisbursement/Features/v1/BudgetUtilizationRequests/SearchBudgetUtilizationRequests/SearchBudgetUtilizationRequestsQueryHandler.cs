using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.BudgetUtilizationRequests.SearchBudgetUtilizationRequests;

public sealed class SearchBudgetUtilizationRequestsQueryHandler(
    BudgetDisbursementDbContext dbContext) : IQueryHandler<SearchBudgetUtilizationRequestsQuery, BudgetUtilizationRequestSearchResult>
{
    public async ValueTask<BudgetUtilizationRequestSearchResult> Handle(SearchBudgetUtilizationRequestsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.BudgetUtilizationRequests.AsNoTracking();

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
            .Select(x => new BudgetUtilizationRequestListItemDto(
                x.Id,
                x.BurNumber,
                x.BurDate,
                x.PurchaseOrderNumber,
                x.AllotmentClass,
                x.Amount,
                x.Status,
                dbContext.SignedDocuments.Any(s =>
                    s.DocumentType == BudgetDisbursementDocumentType.BudgetUtilizationRequest && s.DocumentId == x.Id)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BudgetUtilizationRequestSearchResult(items, totalCount, query.PageNumber, query.PageSize);
    }
}

