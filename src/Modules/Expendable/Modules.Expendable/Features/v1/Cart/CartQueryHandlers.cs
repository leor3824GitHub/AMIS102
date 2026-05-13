using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Cart;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Cart;

public sealed class GetEmployeeCartQueryHandler : IQueryHandler<GetEmployeeCartQuery, EmployeeShoppingCartDto?>
{
    private readonly ExpendableDbContext _dbContext;

    public GetEmployeeCartQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EmployeeShoppingCartDto?> Handle(GetEmployeeCartQuery query, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.ShoppingCarts
            .Where(c => c.EmployeeId == query.EmployeeId && c.Status == CartStatus.Active)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return cart?.ToEmployeeShoppingCartDto();
    }
}



