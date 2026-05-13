using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;

public sealed class RegisterTangibleItemCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<RegisterTangibleItemCommand, TangibleItemDto>
{
    public async ValueTask<TangibleItemDto> Handle(
        RegisterTangibleItemCommand command,
        CancellationToken cancellationToken)
    {
        var propertyNoInUse = await dbContext.TangibleItems
            .AnyAsync(x => x.PropertyNo == command.PropertyNo, cancellationToken)
            .ConfigureAwait(false);

        if (propertyNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(command.PropertyNo),
                    "A tangible item with this PropertyNo already exists.")
            ]);
        }

        var item = await dbContext.PropertyItemCatalog
            .FirstOrDefaultAsync(x => x.Id == command.ItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Item catalog entry with ID {command.ItemId} not found.");
        }

        var tenantId = currentUser.GetTenant() ?? string.Empty;

        var tangibleItem = TangibleItem.Create(
            tenantId,
            command.ItemId,
            command.PropertyNo,
            command.PropertyClass,
            command.CategoryCode,
            command.AcquisitionDate,
            command.Quantity,
            command.UnitCost,
            command.Remarks,
            command.PurchaseOrderId);

        tangibleItem.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.TangibleItems.Add(tangibleItem);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TangibleItemDto(
            tangibleItem.Id,
            tangibleItem.PropertyNo,
            tangibleItem.PropertyClass,
            tangibleItem.CategoryCode,
            tangibleItem.ItemId,
            item.Code,
            item.Name,
            tangibleItem.AcquisitionDate,
            tangibleItem.Quantity,
            tangibleItem.UnitCost,
            tangibleItem.Remarks,
            tangibleItem.PurchaseOrderId,
            tangibleItem.CreatedOnUtc);
    }
}

