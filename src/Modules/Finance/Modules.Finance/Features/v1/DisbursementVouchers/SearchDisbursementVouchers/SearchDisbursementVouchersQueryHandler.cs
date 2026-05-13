using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.Finance.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.SearchDisbursementVouchers;

public sealed class SearchDisbursementVouchersQueryHandler(
    FinanceDbContext dbContext) : IQueryHandler<SearchDisbursementVouchersQuery, DisbursementVoucherSearchResult>
{
    public async ValueTask<DisbursementVoucherSearchResult> Handle(SearchDisbursementVouchersQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.DisbursementVouchers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.DvNumber.ToLower().Contains(kw) ||
                x.Payee.ToLower().Contains(kw) ||
                x.PurchaseOrderNumber.ToLower().Contains(kw) ||
                x.Particulars.ToLower().Contains(kw));
        }

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.PurchaseOrderId.HasValue)
            q = q.Where(x => x.PurchaseOrderId == query.PurchaseOrderId.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.DvDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new DisbursementVoucherListItemDto(
                x.Id,
                x.DvNumber,
                x.DvDate,
                x.PurchaseOrderNumber,
                x.Payee,
                x.Amount,
                x.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DisbursementVoucherSearchResult(items, totalCount, query.PageNumber, query.PageSize);
    }
}

