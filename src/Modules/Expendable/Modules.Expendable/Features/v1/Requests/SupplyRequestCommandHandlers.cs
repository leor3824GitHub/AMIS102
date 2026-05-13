using AMIS.Framework.Caching;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Requests;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Requests;

public sealed class MarkSupplyRequestFulfilledCommandHandler : ICommandHandler<MarkSupplyRequestFulfilledCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public MarkSupplyRequestFulfilledCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(MarkSupplyRequestFulfilledCommand command, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Supply request {command.Id} not found.");

        request.MarkFulfilled();
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}

public sealed class CancelSupplyRequestCommandHandler : ICommandHandler<CancelSupplyRequestCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheService _cache;

    public CancelSupplyRequestCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser, ICacheService cache)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async ValueTask<Unit> Handle(CancelSupplyRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Supply request {command.Id} not found.");

        var wasApproved = request.Status == SupplyRequestStatus.Approved;
        var warehouseLocationId = request.WarehouseLocationId;

        request.Cancel();
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        // Release warehouse reservations if cancelling an approved request
        if (wasApproved && warehouseLocationId.HasValue)
        {
            var approvedProductIds = request.Items
                .Where(i => i.ApprovedQuantity > 0)
                .Select(i => i.ProductId)
                .ToList();

            if (approvedProductIds.Count > 0)
            {
                var inventories = await _dbContext.ProductInventories
                    .Where(pi => approvedProductIds.Contains(pi.ProductId)
                              && pi.WarehouseLocationId == warehouseLocationId.Value)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var item in request.Items.Where(i => i.ApprovedQuantity > 0))
                {
                    var inventory = inventories.FirstOrDefault(pi => pi.ProductId == item.ProductId);
                    if (inventory == null) continue;

                    inventory.CancelReservation(item.ApprovedQuantity);

                    await _cache.RemoveItemAsync($"inventory:{item.ProductId}:{warehouseLocationId}", cancellationToken);
                    await _cache.RemoveItemAsync($"inventory:{inventory.Id}", cancellationToken);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}




