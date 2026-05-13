using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.RecordPurchaseReceipt;

public sealed class RecordPurchaseReceiptCommandHandler : ICommandHandler<RecordPurchaseReceiptCommand>
{
    private readonly ExpendableDbContext _dbContext;

    public RecordPurchaseReceiptCommandHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(RecordPurchaseReceiptCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .FirstOrDefaultAsync(p => p.Id == command.PurchaseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Purchase {command.PurchaseId} not found.");

        purchase.RecordReceipt(command.ProductId, command.ReceivedQuantity, command.RejectedQuantity);

        purchase.ReceivingNotes = command.ReceivingNotes;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}

