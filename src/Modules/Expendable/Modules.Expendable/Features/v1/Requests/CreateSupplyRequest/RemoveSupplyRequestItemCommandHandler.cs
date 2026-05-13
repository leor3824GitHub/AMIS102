using AMIS.Framework.Core.Context;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;

public sealed class RemoveSupplyRequestItemCommandHandler : ICommandHandler<RemoveSupplyRequestItemCommand>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RemoveSupplyRequestItemCommandHandler(ExpendableDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(RemoveSupplyRequestItemCommand command, CancellationToken cancellationToken)
    {
        var request = await _dbContext.SupplyRequests
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Supply request {command.RequestId} not found.");

        request.RemoveItem(command.ProductId);
        request.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return default;
    }
}

