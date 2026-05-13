using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.ReserveProductInventory;

public sealed class ReserveProductInventoryCommandHandler : ICommandHandler<ReserveProductInventoryCommand, ReserveProductInventoryResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public ReserveProductInventoryCommandHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<ReserveProductInventoryResponse> Handle(ReserveProductInventoryCommand command, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.ProductInventories
            .FirstOrDefaultAsync(pi => pi.Id == command.ProductInventoryId, cancellationToken)
            ?? throw new InvalidOperationException($"ProductInventory {command.ProductInventoryId} not found");

        inventory.ReserveForAllocation(command.QuantityToReserve);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveItemAsync($"inventory:{inventory.ProductId}:{inventory.WarehouseLocationId}", cancellationToken);
        await _cache.RemoveItemAsync($"inventory:{command.ProductInventoryId}", cancellationToken);

        return new ReserveProductInventoryResponse(
            inventory.Id,
            inventory.QuantityAvailable,
            inventory.QuantityReserved
        );
    }
}

