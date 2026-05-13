using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.SubmitPurchaseOrder;

public sealed class SubmitPurchaseOrderCommandHandler : ICommandHandler<SubmitPurchaseOrderCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SubmitPurchaseOrderCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Purchase {command.Id} not found.");

        purchase.Submit();
        purchase.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}

