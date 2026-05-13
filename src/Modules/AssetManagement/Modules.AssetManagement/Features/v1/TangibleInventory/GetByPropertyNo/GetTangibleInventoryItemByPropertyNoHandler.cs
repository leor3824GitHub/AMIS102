using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.GetByPropertyNo;

public sealed class GetTangibleInventoryItemByPropertyNoHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetTangibleInventoryItemByPropertyNoQuery, TangibleInventoryItemDetailDto>
{
    public async ValueTask<TangibleInventoryItemDetailDto> Handle(
        GetTangibleInventoryItemByPropertyNoQuery query,
        CancellationToken cancellationToken)
    {
        var propertyNo = query.PropertyNo.Trim().ToUpperInvariant();

        var item = await dbContext.TangibleInventoryItems
            .AsNoTracking()
            .Where(x => x.PropertyNo == propertyNo)
            .Select(x => new
            {
                x.Id,
                x.PropertyNo,
                x.Description,
                x.UnitCost,
                x.AssetType,
                x.IsIssued,
                x.ItemId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
            throw new NotFoundException($"Property number '{propertyNo}' not found.");

        var itemName = await dbContext.PropertyItemCatalog
            .AsNoTracking()
            .Where(x => x.Id == item.ItemId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        string? linkedDocumentType = null;
        string? linkedDocumentNo = null;
        Guid? linkedDocumentId = null;

        if (item.IsIssued)
        {
            if (item.AssetType == AssetType.SE)
            {
                var icsLink = await dbContext.ICSItems
                    .AsNoTracking()
                    .Where(x => x.TangibleInventoryItemId == item.Id)
                    .Join(
                        dbContext.InventoryCustodianSlips.AsNoTracking(),
                        icsItem => icsItem.ICSId,
                        ics => ics.Id,
                        (icsItem, ics) => new { ics.Id, ics.ICSNo })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (icsLink is not null)
                {
                    linkedDocumentType = "ICS";
                    linkedDocumentNo = icsLink.ICSNo;
                    linkedDocumentId = icsLink.Id;
                }
            }
            else if (item.AssetType == AssetType.PPE)
            {
                var parLink = await dbContext.PARItems
                    .AsNoTracking()
                    .Where(x => x.TangibleInventoryItemId == item.Id)
                    .Join(
                        dbContext.PropertyAcknowledgementReceipts.AsNoTracking(),
                        parItem => parItem.PARId,
                        par => par.Id,
                        (parItem, par) => new { par.Id, par.PARNo })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (parLink is not null)
                {
                    linkedDocumentType = "PAR";
                    linkedDocumentNo = parLink.PARNo;
                    linkedDocumentId = parLink.Id;
                }
            }
        }

        return new TangibleInventoryItemDetailDto(
            item.Id,
            item.PropertyNo,
            itemName,
            item.Description,
            item.UnitCost,
            item.AssetType.ToString(),
            item.IsIssued,
            linkedDocumentType,
            linkedDocumentNo,
            linkedDocumentId);
    }
}

