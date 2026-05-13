using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;

public sealed class CreateRRSPCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreateRRSPCommand, CreateRRSPResult>
{
    public async ValueTask<CreateRRSPResult> Handle(CreateRRSPCommand command, CancellationToken cancellationToken)
    {
        // 1. Validate RRSPNo uniqueness.
        var rrspNoExists = await dbContext.ReceiptForReturnedProperties
            .AnyAsync(x => x.RRSPNo == command.RRSPNo, cancellationToken)
            .ConfigureAwait(false);

        if (rrspNoExists)
        {
            throw new InvalidOperationException($"RRSP number '{command.RRSPNo}' already exists.");
        }

        // 2. Load the ICS — must be Active.
        var ics = await dbContext.InventoryCustodianSlips
            .FirstOrDefaultAsync(x => x.Id == command.ICSId, cancellationToken)
            .ConfigureAwait(false);

        if (ics is null)
        {
            throw new NotFoundException($"Inventory Custodian Slip with ID {command.ICSId} not found.");
        }

        if (ics.Status != ICSStatus.Active)
        {
            throw new InvalidOperationException(
                $"ICS '{ics.ICSNo}' is not Active (current status: {ics.Status}). " +
                "Only Active ICS records can be cancelled by an RRSP.");
        }

        // 3. Load all items on this ICS.
        var icsItems = await dbContext.ICSItems
            .Where(x => x.ICSId == command.ICSId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (icsItems.Count == 0)
        {
            throw new InvalidOperationException($"ICS '{ics.ICSNo}' has no items.");
        }

        // 4. Load the corresponding TangibleInventoryItems.
        var invItemIds = icsItems.Select(x => x.TangibleInventoryItemId).ToList();

        var invItems = await dbContext.TangibleInventoryItems
            .Where(x => invItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await dbContext.AssetRegistry
            .Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        // 5. Create the RRSP header.
        string tenantId = currentUser.GetTenant() ?? string.Empty;
        string userId = currentUser.GetUserId().ToString();

        var rrsp = ReceiptForReturnedProperty.Create(
            tenantId,
            command.RRSPNo,
            command.Date,
            command.ICSId,
            command.FundCluster,
            command.ReceivedByEmployeeId,
            command.ReturnedByEmployeeId,
            command.Remarks);

        rrsp.CreatedBy = userId;
        dbContext.ReceiptForReturnedProperties.Add(rrsp);

        // 6. Create RRSP items and mark each inventory item returned.
        for (var i = 0; i < icsItems.Count; i++)
        {
            var icsItem = icsItems[i];

            if (!invItems.TryGetValue(icsItem.TangibleInventoryItemId, out var invItem))
            {
                throw new NotFoundException(
                    $"TangibleInventoryItem with ID {icsItem.TangibleInventoryItemId} not found.");
            }

            var rrspItem = RRSPItem.Create(
                tenantId: tenantId,
                rrspId: rrsp.Id,
                tangibleInventoryItemId: icsItem.TangibleInventoryItemId,
                itemNo: i + 1,
                description: icsItem.Description,
                unitCost: icsItem.UnitCost,
                assetTypeAtTimeOfReturn: icsItem.AssetTypeAtTimeOfIssuance);

            dbContext.RRSPItems.Add(rrspItem);

            invItem.MarkReturned();

            if (!registryByInventoryItemId.TryGetValue(invItem.Id, out var registry))
            {
                registry = AssetRegistry.Create(
                    tenantId: invItem.TenantId,
                    tangibleInventoryItemId: invItem.Id,
                    itemId: invItem.ItemId,
                    propertyNo: invItem.PropertyNo,
                    assetType: invItem.AssetType,
                    acquisitionDate: invItem.AcquisitionDate,
                    unitCost: invItem.UnitCost);

                dbContext.AssetRegistry.Add(registry);
                registryByInventoryItemId[invItem.Id] = registry;
            }

            var previousCustodian = registry.CurrentCustodianId;
            registry.ReturnToStock();

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.Returned,
                DateTimeOffset.UtcNow,
                "RRSP",
                rrsp.Id,
                rrsp.RRSPNo,
                previousCustodian,
                null,
                null,
                command.Remarks);

            dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
        }

        // 7. Cancel the ICS.
        ics.CancelByReturn(rrsp.Id);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateRRSPResult(rrsp.Id, rrsp.RRSPNo, icsItems.Count);
    }
}
