using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.DeleteFundCluster;

public sealed class DeleteFundClusterCommandHandler : ICommandHandler<DeleteFundClusterCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteFundClusterCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteFundClusterCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundClusters
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new NotFoundException($"Fund cluster with ID {command.Id} not found.");
        }

        entity.SoftDelete(_currentUser.GetUserId().ToString());

        _dbContext.FundClusters.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
