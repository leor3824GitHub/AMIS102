using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public sealed class GetPropertyItemCatalogByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPropertyItemCatalogByIdQuery, PropertyItemCatalogDetailsDto>
{
    public async ValueTask<PropertyItemCatalogDetailsDto> Handle(GetPropertyItemCatalogByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await dbContext.PropertyItemCatalog
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Item catalog entry with ID {query.Id} not found.");
        }

        return new PropertyItemCatalogDetailsDto(
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
