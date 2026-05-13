using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.GetPurchase;

public sealed class GetPurchaseQueryHandler : IQueryHandler<GetPurchaseQuery, PurchaseDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetPurchaseQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PurchaseDto?> Handle(GetPurchaseQuery query, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);
        return purchase?.ToPurchaseDto();
    }
}

