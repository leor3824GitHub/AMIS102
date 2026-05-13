using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryDisposed;

public sealed class MarkRejectedInventoryDisposedCommandHandler : ICommandHandler<MarkRejectedInventoryDisposedCommand, MarkRejectedInventoryDisposedResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public MarkRejectedInventoryDisposedCommandHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<MarkRejectedInventoryDisposedResponse> Handle(MarkRejectedInventoryDisposedCommand command, CancellationToken cancellationToken)
    {
        var rejected = await _dbContext.RejectedInventories
            .FirstOrDefaultAsync(ri => ri.Id == command.RejectedInventoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Rejected inventory {command.RejectedInventoryId} not found");

        rejected.MarkAsDisposed(command.DisposalMethod, command.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveItemAsync($"rejected-inventory:{command.RejectedInventoryId}", cancellationToken);

        return new MarkRejectedInventoryDisposedResponse(rejected.Id, rejected.Status.ToString());
    }
}

