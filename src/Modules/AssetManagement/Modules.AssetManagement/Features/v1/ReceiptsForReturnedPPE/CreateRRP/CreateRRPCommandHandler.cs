using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

public sealed class CreateRRPCommandHandler : ICommandHandler<CreateRRPCommand, CreateRRPResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateRRPCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreateRRPResult> Handle(CreateRRPCommand command, CancellationToken cancellationToken)
    {
        var rrpNoInUse = await _dbContext.ReceiptsForReturnedPPE
            .AnyAsync(x => x.RRPNo == command.RRPNo, cancellationToken)
            .ConfigureAwait(false);

        if (rrpNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.RRPNo), "An RRP with this number already exists.")
            ]);
        }

        var requestedIds = command.Items.Select(x => x.TangibleInventoryItemId).Distinct().ToList();

        if (requestedIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate inventory item entries are not allowed in a single RRP.")
            ]);
        }

        var invItems = await _dbContext.TangibleInventoryItems
            .Where(x => requestedIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => requestedIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedIds)
        {
            if (!invItems.TryGetValue(itemId, out var invItem))
                throw new KeyNotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (invItem.AssetType != AssetType.PPE)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has AssetType '{invItem.AssetType}'. Only PPE items can be returned via RRP.");

            if (!invItem.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} is not currently issued and cannot be returned.");
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        var rrp = ReceiptForReturnedPPE.Create(
            tenantId,
            command.RRPNo,
            command.Date,
            command.ReturnCategory,
            command.ReturnedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.SignedByEmployeeId,
            command.PropertyInspectorCertified);

        rrp.CreatedBy = userId;
        _dbContext.ReceiptsForReturnedPPE.Add(rrp);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var rrpItem = RRPItem.Create(
                tenantId,
                rrp.Id,
                invItem.Id,
                itemNo,
                itemRequest.SourceDocumentRef,
                invItem.PropertyNo,
                invItem.Description ?? string.Empty,
                itemRequest.Quantity,
                invItem.UnitCost);

            _dbContext.RRPItems.Add(rrpItem);

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

                _dbContext.AssetRegistry.Add(registry);
                registryByInventoryItemId[invItem.Id] = registry;
            }

            var previousCustodian = registry.CurrentCustodianId;

            if (command.ReturnCategory == PPEReturnCategory.Serviceable)
            {
                invItem.MarkReturned();
                registry.ReturnToStock();

                var history = AssetAssignmentHistory.Create(
                    tenantId,
                    registry.Id,
                    AssetAssignmentEventType.Returned,
                    DateTimeOffset.UtcNow,
                    "RRP",
                    rrp.Id,
                    rrp.RRPNo,
                    previousCustodian,
                    null,
                    null,
                    "Serviceable PPE returned to stock.");

                _dbContext.AssetAssignmentHistory.Add(history);
                registry.LinkCurrentAssignment(history.Id);
            }
            else
            {
                invItem.MarkReturned();
                registry.MarkDisposed();

                var history = AssetAssignmentHistory.Create(
                    tenantId,
                    registry.Id,
                    AssetAssignmentEventType.StatusChanged,
                    DateTimeOffset.UtcNow,
                    "RRP",
                    rrp.Id,
                    rrp.RRPNo,
                    previousCustodian,
                    null,
                    null,
                    "Junked PPE marked as disposed.");

                _dbContext.AssetAssignmentHistory.Add(history);
                registry.LinkCurrentAssignment(history.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateRRPResult(rrp.Id, rrp.RRPNo, command.Items.Count);
    }
}

