using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.AddPurchaseLineItem;

public sealed class AddPurchaseLineItemCommandHandler : ICommandHandler<AddPurchaseLineItemCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AddPurchaseLineItemCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(AddPurchaseLineItemCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .FirstOrDefaultAsync(p => p.Id == command.PurchaseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Purchase {command.PurchaseId} not found.");

        purchase.AddLineItem(command.ProductId, command.ProductCode, command.ProductName, command.Quantity, command.UnitPrice);
        purchase.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}

