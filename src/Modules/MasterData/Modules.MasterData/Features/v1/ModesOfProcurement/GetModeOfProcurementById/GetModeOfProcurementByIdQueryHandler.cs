using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.ModesOfProcurement.GetModeOfProcurementById;

public sealed class GetModeOfProcurementByIdQueryHandler : IQueryHandler<GetModeOfProcurementByIdQuery, ModeOfProcurementDetailsDto>
{
    private readonly MasterDataDbContext _dbContext;

    public GetModeOfProcurementByIdQueryHandler(MasterDataDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ModeOfProcurementDetailsDto> Handle(GetModeOfProcurementByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ModesOfProcurement
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new KeyNotFoundException($"Mode of procurement with ID {query.Id} not found.");
        }

        return new ModeOfProcurementDetailsDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.CreatedBy,
            entity.LastModifiedOnUtc,
            entity.LastModifiedBy);
    }
}

