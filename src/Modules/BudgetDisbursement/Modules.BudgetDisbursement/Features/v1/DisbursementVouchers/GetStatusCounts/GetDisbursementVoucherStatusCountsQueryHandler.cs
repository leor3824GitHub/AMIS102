using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.BudgetDisbursement.Features.v1.DisbursementVouchers.GetStatusCounts;

public sealed class GetDisbursementVoucherStatusCountsQueryHandler(BudgetDisbursementDbContext dbContext)
    : IQueryHandler<GetDisbursementVoucherStatusCountsQuery, IReadOnlyList<DisbursementVoucherStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<DisbursementVoucherStatusCountDto>> Handle(
        GetDisbursementVoucherStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.DisbursementVouchers.AsNoTracking();

        // Mirror SearchDisbursementVouchersQueryHandler's keyword filter so the badge counts
        // match exactly what the list returns for the same search.
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.DvNumber.ToLower().Contains(kw) ||
                x.Payee.ToLower().Contains(kw) ||
                x.PurchaseOrderNumber.ToLower().Contains(kw) ||
                x.BurNumber.ToLower().Contains(kw) ||
                x.Particulars.ToLower().Contains(kw));
        }

        return await q
            .GroupBy(x => x.Status)
            .Select(g => new DisbursementVoucherStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
