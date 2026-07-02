using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodeById;

public sealed class GetFundingSourceCodeByIdQueryHandler : IQueryHandler<GetFundingSourceCodeByIdQuery, FundingSourceCodeDetailsDto>
{
    private readonly MasterDataDbContext _dbContext;

    public GetFundingSourceCodeByIdQueryHandler(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<FundingSourceCodeDetailsDto> Handle(GetFundingSourceCodeByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.FundingSourceCodes
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Funding source code with ID {query.Id} not found.");
        }

        return new FundingSourceCodeDetailsDto(
            entity.Id,
            entity.Code,
            entity.FundClusterCode,
            entity.FinancingSource,
            entity.Authorization,
            entity.FundCategory,
            entity.FundSubCategory,
            entity.Description,
            entity.DepartmentName,
            entity.AgencyName,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.CreatedBy,
            entity.LastModifiedOnUtc,
            entity.LastModifiedBy);
    }
}
