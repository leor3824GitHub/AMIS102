using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEItems.GetPPEItemById;

public sealed class GetPPEItemByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPEItemByIdQuery, PPEItemDetailsDto>
{
    public async ValueTask<PPEItemDetailsDto> Handle(GetPPEItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await dbContext.PPEItems
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"PPE item with ID {query.Id} not found.");
        }

        return new PPEItemDetailsDto(
            item.Id,
            item.PropertyCode,
            item.PropertyNumber,
            item.Description,
            item.SerialNumber,
            item.DateAcquired,
            item.UnitCost,
            item.EstimatedUsefulLifeYears,
            item.Status.ToString(),
            item.CurrentAccountableEmployeeId,
            item.SourcePPERRId,
            item.CreatedOnUtc,
            item.CreatedBy);
    }
}
