using FSH.Framework.Caching;
using FSH.Framework.Core.Context;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Expendable.Features.v1.Requests.ApproveSupplyRequest;

public sealed class ApproveSupplyRequestCommandHandler : ICommandHandler<ApproveSupplyRequestCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;

    public ApproveSupplyRequestCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser, ICacheService cache)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async ValueTask<Unit> Handle(ApproveSupplyRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Supply request {command.Id} not found.");

        // Approve items and store the warehouse location on the request
        request.Approve(_currentUser.GetUserId().ToString(), command.ApprovedQuantities, command.WarehouseLocationId);
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        // Auto-reserve approved quantities from the specified warehouse
        var approvedProductIds = command.ApprovedQuantities
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => kvp.Key)
            .ToList();

        if (approvedProductIds.Count > 0)
        {
            var productInventories = await _dbContext.ProductInventories
                .Where(pi => approvedProductIds.Contains(pi.ProductId)
                          && pi.WarehouseLocationId == command.WarehouseLocationId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var (productId, approvedQty) in command.ApprovedQuantities.Where(kvp => kvp.Value > 0))
            {
                var inventory = productInventories.FirstOrDefault(pi => pi.ProductId == productId)
                    ?? throw new InvalidOperationException(
                        $"No warehouse inventory found for product {productId} at warehouse {command.WarehouseLocationId}. " +
                        "Ensure the product has been received and inspected at this warehouse.");

                inventory.ReserveForAllocation(approvedQty);

                await _cache.RemoveItemAsync($"inventory:{productId}:{command.WarehouseLocationId}", cancellationToken);
                await _cache.RemoveItemAsync($"inventory:{inventory.Id}", cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}
