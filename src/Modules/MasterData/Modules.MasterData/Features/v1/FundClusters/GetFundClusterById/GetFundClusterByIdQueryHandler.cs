using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.GetFundClusterById;

public sealed class GetFundClusterByIdQueryHandler : IQueryHandler<GetFundClusterByIdQuery, FundClusterDetailsDto>
{
    private readonly MasterDataDbContext _dbContext;

    public GetFundClusterByIdQueryHandler(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<FundClusterDetailsDto> Handle(GetFundClusterByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundClusters
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Fund cluster with ID {query.Id} not found.");
        }

        return new FundClusterDetailsDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.CreatedBy,
            entity.LastModifiedOnUtc,
            entity.LastModifiedBy);
    }
}
