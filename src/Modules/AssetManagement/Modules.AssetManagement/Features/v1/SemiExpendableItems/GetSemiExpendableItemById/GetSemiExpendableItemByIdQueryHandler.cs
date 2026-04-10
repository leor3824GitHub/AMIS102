using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public sealed class GetSemiExpendableItemByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSemiExpendableItemByIdQuery, SemiExpendableItemDetailsDto>
{
    public async ValueTask<SemiExpendableItemDetailsDto> Handle(GetSemiExpendableItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await dbContext.SemiExpendableItems
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Semi-expendable item with ID {query.Id} not found.");
        }

        return new SemiExpendableItemDetailsDto(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.UACSObjectCode,
            item.UnitOfMeasure,
            item.EstimatedUsefulLifeYears,
            item.IsActive,
            item.CreatedOnUtc,
            item.CreatedBy,
            item.LastModifiedOnUtc,
            item.LastModifiedBy);
    }
}
