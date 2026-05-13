using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Cart;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Cart.GetCart;

public sealed class GetCartQueryHandler : IQueryHandler<GetCartQuery, EmployeeShoppingCartDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetCartQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EmployeeShoppingCartDto?> Handle(GetCartQuery query, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.ShoppingCarts
            .FirstOrDefaultAsync(c => c.Id == query.CartId, cancellationToken)
            .ConfigureAwait(false);
        return cart?.ToEmployeeShoppingCartDto();
    }
}

