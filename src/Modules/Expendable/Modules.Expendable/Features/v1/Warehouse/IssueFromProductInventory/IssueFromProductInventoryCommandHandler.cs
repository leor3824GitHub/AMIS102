using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.IssueFromProductInventory;

public sealed class IssueFromProductInventoryCommandHandler : ICommandHandler<IssueFromProductInventoryCommand, IssueFromProductInventoryResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public IssueFromProductInventoryCommandHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<IssueFromProductInventoryResponse> Handle(IssueFromProductInventoryCommand command, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.ProductInventories
            .FirstOrDefaultAsync(pi => pi.Id == command.ProductInventoryId, cancellationToken)
            ?? throw new InvalidOperationException($"ProductInventory {command.ProductInventoryId} not found");

        var issued = inventory.IssueReservedStock(command.QuantityToIssue);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveItemAsync($"inventory:{inventory.ProductId}:{inventory.WarehouseLocationId}", cancellationToken);
        await _cache.RemoveItemAsync($"inventory:{command.ProductInventoryId}", cancellationToken);

        var response = new IssueFromProductInventoryResponse(
            inventory.Id,
            command.QuantityToIssue,
            issued.UnitPrice,
            issued.TotalValue
        );

        return response;
    }
}

