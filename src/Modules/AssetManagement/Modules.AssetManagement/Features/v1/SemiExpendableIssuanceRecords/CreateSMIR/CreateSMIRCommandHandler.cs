using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;

public sealed class CreateSMIRCommandHandler : ICommandHandler<CreateSMIRCommand, CreateSMIRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateSMIRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreateSMIRResult> Handle(CreateSMIRCommand command, CancellationToken cancellationToken)
    {
        var smirNoInUse = await _dbContext.SemiExpendableIssuanceRecords
            .AnyAsync(x => x.SMIRNo == command.SMIRNo, cancellationToken)
            .ConfigureAwait(false);

        if (smirNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.SMIRNo), "A SMIR with this number already exists.")
            ]);
        }

        var invItemIds = command.Items.Select(x => x.TangibleInventoryItemId).Distinct().ToList();

        if (invItemIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate inventory item entries are not allowed in a single SMIR.")
            ]);
        }

        var invItems = await _dbContext.TangibleInventoryItems
            .Where(x => invItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => invItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in invItemIds)
        {
            if (!invItems.TryGetValue(itemId, out var invItem))
                throw new NotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (invItem.AssetType != AssetType.SE)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has AssetType '{invItem.AssetType}'. Only SE items can be transferred via SMIR.");

            if (invItem.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} is currently issued and cannot be transferred via SMIR. " +
                    "The employee must first return it via RRSP.");

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

        var smir = SemiExpendableIssuanceRecord.Create(
            tenantId,
            command.SMIRNo,
            command.Date,
            command.FundCluster,
            command.IssuanceType,
            command.TransferredToTenantId,
            command.TransferredToOfficerName,
            command.IssuedByEmployeeId,
            command.Remarks);

        smir.CreatedBy = userId;
        _dbContext.SemiExpendableIssuanceRecords.Add(smir);

        int itemNo = 1;
        foreach (var itemRequest in command.Items)
        {
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var smirItem = SMIRItem.Create(
                tenantId,
                smir.Id,
                invItem.Id,
                itemNo,
                itemRequest.Description ?? invItem.PropertyNo,
                invItem.UnitCost,
                invItem.AssetType);

            _dbContext.SMIRItems.Add(smirItem);

            // Mark the inventory item as issued — transferred out of the current tenant.
            invItem.MarkIssued();

            var registry = registryByInventoryItemId[invItem.Id];
            var previousCustodian = registry.CurrentCustodianId;
            registry.TransferOut();

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.Transferred,
                DateTimeOffset.UtcNow,
                "SMIR",
                smir.Id,
                smir.SMIRNo,
                previousCustodian,
                null,
                null,
                command.Remarks);

            _dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);

            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateSMIRResult(smir.Id, smir.SMIRNo, invItemIds.Count);
    }
}

