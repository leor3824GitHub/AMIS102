using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.UpdateTangibleItem;

public sealed class UpdateTangibleItemCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdateTangibleItemCommand, TangibleItemDto>
{
    public async ValueTask<TangibleItemDto> Handle(
        UpdateTangibleItemCommand command,
        CancellationToken cancellationToken)
    {
        var tangibleItem = await dbContext.TangibleItems
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tangibleItem is null)
        {
            throw new KeyNotFoundException($"Tangible item with ID {command.Id} not found.");
        }

        tangibleItem.Update(
            command.AcquisitionDate,
            command.Quantity,
            command.UnitCost,
            command.Remarks,
            command.PurchaseOrderId);

        tangibleItem.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TangibleItemDto(
            tangibleItem.Id,
            tangibleItem.PropertyNo,
            tangibleItem.PropertyClass,
            tangibleItem.CategoryCode,
            tangibleItem.ItemId,
            tangibleItem.Item.Code,
            tangibleItem.Item.Name,
            tangibleItem.AcquisitionDate,
            tangibleItem.Quantity,
            tangibleItem.UnitCost,
            tangibleItem.Remarks,
            tangibleItem.PurchaseOrderId,
            tangibleItem.CreatedOnUtc);
    }
}
