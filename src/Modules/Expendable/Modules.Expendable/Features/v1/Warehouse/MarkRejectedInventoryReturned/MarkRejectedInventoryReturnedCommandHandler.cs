using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryReturned;

public sealed class MarkRejectedInventoryReturnedCommandHandler : ICommandHandler<MarkRejectedInventoryReturnedCommand, MarkRejectedInventoryReturnedResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public MarkRejectedInventoryReturnedCommandHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<MarkRejectedInventoryReturnedResponse> Handle(MarkRejectedInventoryReturnedCommand command, CancellationToken cancellationToken)
    {
        var rejected = await _dbContext.RejectedInventories
            .FirstOrDefaultAsync(ri => ri.Id == command.RejectedInventoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Rejected inventory {command.RejectedInventoryId} not found");

        rejected.MarkAsReturned(command.QuantityReturned, command.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveItemAsync($"rejected-inventory:{command.RejectedInventoryId}", cancellationToken);

        return new MarkRejectedInventoryReturnedResponse(rejected.Id, rejected.Status.ToString());
    }
}

