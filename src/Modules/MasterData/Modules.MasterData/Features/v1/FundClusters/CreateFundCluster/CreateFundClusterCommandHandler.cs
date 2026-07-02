using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.CreateFundCluster;

public sealed class CreateFundClusterCommandHandler : ICommandHandler<CreateFundClusterCommand, FundClusterDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateFundClusterCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<FundClusterDto> Handle(CreateFundClusterCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.FundClusters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A fund cluster with this code already exists.")
            ]);
        }

        var entity = FundCluster.Create(command.Code, command.Name, command.Description);
        entity.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.FundClusters.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new FundClusterDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.IsActive);
    }
}
