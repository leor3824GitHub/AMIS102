using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

public sealed class CreateICSCommandHandler : ICommandHandler<CreateICSCommand, CreateICSResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateICSCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreateICSResult> Handle(CreateICSCommand command, CancellationToken cancellationToken)
    {
        var icsNoInUse = await _dbContext.InventoryCustodianSlips
            .AnyAsync(x => x.ICSNo == command.ICSNo, cancellationToken)
            .ConfigureAwait(false);

        if (icsNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.ICSNo), "An ICS with this number already exists.")
            ]);
        }

        var itemIds = command.Items.Select(x => x.TangibleInventoryItemId).Distinct().ToList();

        if (itemIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate inventory item entries are not allowed in a single ICS.")
            ]);
        }

        var invItems = await _dbContext.TangibleInventoryItems
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => itemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in itemIds)
        {
            if (!invItems.TryGetValue(itemId, out var invItem))
                throw new KeyNotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (invItem.AssetType != AssetType.SE)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has AssetType '{invItem.AssetType}'. Only SE items can be issued via ICS.");

            if (invItem.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} is already issued.");

            if (!registryByInventoryItemId.ContainsKey(itemId))
            {
                var registry = AssetRegistry.Create(
                    tenantId: invItem.TenantId,
                    tangibleInventoryItemId: invItem.Id,
                    itemId: invItem.ItemId,
                    propertyNo: invItem.PropertyNo,
                    assetType: invItem.AssetType,
                    acquisitionDate: invItem.AcquisitionDate,
                    unitCost: invItem.UnitCost);

                _dbContext.AssetRegistry.Add(registry);
                registryByInventoryItemId[itemId] = registry;
            }
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        var ics = InventoryCustodianSlip.Create(
            tenantId,
            command.ICSNo,
            command.Date,
            command.Category,
            command.FundCluster,
            command.IssuedFromEmployeeId,
            command.ReceivedByEmployeeId);

        ics.CreatedBy = userId;
        _dbContext.InventoryCustodianSlips.Add(ics);

        int itemNo = 1;
        foreach (var itemRequest in command.Items)
        {
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var icsItem = ICSItem.Create(
                tenantId,
                ics.Id,
                invItem.Id,
                itemNo,
                itemRequest.Description ?? invItem.Description,
                invItem.UnitCost,
                null, // EUL not on TangibleInventoryItem — may be sourced from catalog separately
                invItem.AssetType);

            _dbContext.ICSItems.Add(icsItem);
            invItem.MarkIssued();

            var registry = registryByInventoryItemId[invItem.Id];
            var previousCustodian = registry.CurrentCustodianId;
            registry.AssignTo(command.ReceivedByEmployeeId, null);
            var eventType = previousCustodian.HasValue
                ? AssetAssignmentEventType.Transferred
                : AssetAssignmentEventType.Assigned;

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                eventType,
                DateTimeOffset.UtcNow,
                "ICS",
                ics.Id,
                ics.ICSNo,
                previousCustodian,
                command.ReceivedByEmployeeId,
                null,
                null);

            _dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateICSResult(ics.Id, ics.ICSNo, command.Items.Count);
    }
}

