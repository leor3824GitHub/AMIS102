using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.UpdateFundCluster;

public sealed class UpdateFundClusterCommandHandler : ICommandHandler<UpdateFundClusterCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateFundClusterCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateFundClusterCommand command, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundClusters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Fund cluster with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.FundClusters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A fund cluster with this code already exists.")
            ]);
        }

        entity.Update(command.Code, command.Name, command.Description, command.IsActive);
        entity.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.FundClusters.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
