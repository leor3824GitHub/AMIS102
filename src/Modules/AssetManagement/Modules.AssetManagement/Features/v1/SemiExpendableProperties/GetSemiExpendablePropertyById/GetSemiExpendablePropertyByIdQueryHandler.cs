using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendablePropertyById;

public sealed class GetSemiExpendablePropertyByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSemiExpendablePropertyByIdQuery, SemiExpendablePropertyDetailsDto>
{
    public async ValueTask<SemiExpendablePropertyDetailsDto> Handle(GetSemiExpendablePropertyByIdQuery query, CancellationToken cancellationToken)
    {
        var property = await dbContext.SemiExpendableProperties
            .Include(x => x.SemiExpendableItem)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (property is null)
        {
            throw new KeyNotFoundException($"Semi-expendable property with ID {query.Id} not found.");
        }

        return new SemiExpendablePropertyDetailsDto(
            property.Id,
            property.PropertyNo,
            property.SemiExpendableItemId,
            property.SemiExpendableItem.Code,
            property.SemiExpendableItem.Name,
            property.SemiExpendableItem.UnitOfMeasure,
            property.SemiExpendableItem.EstimatedUsefulLifeYears,
            property.SerialNo,
            property.AcquisitionDate,
            property.UnitCost,
            property.FundCluster,
            property.Status.ToString(),
            property.CurrentCustodianId,
            property.Remarks,
            property.CreatedOnUtc,
            property.CreatedBy,
            property.LastModifiedOnUtc,
            property.LastModifiedBy);
    }
}
