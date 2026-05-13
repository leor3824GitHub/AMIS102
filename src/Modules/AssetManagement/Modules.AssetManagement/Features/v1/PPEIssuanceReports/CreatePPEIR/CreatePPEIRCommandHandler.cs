using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.CreatePPEIR;

public sealed class CreatePPEIRCommandHandler : ICommandHandler<CreatePPEIRCommand, CreatePPEIRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePPEIRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreatePPEIRResult> Handle(CreatePPEIRCommand command, CancellationToken cancellationToken)
    {
        var ppeirNoInUse = await _dbContext.PPEIssuanceReports
            .AnyAsync(x => x.PPEIRNo == command.PPEIRNo, cancellationToken)
            .ConfigureAwait(false);

        if (ppeirNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PPEIRNo), "A PPEIR with this number already exists.")
            ]);
        }

        var requestedItemIds = command.Items.Select(x => x.TangibleInventoryItemId).Distinct().ToList();

        if (requestedItemIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate inventory item entries are not allowed in a single PPEIR.")
            ]);
        }

        var invItems = await _dbContext.TangibleInventoryItems
            .Where(x => requestedItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => requestedItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedItemIds)
        {
            if (!invItems.TryGetValue(itemId, out var invItem))
                throw new KeyNotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (invItem.AssetType != AssetType.PPE)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has AssetType '{invItem.AssetType}'. Only PPE items can be transferred via PPEIR.");

            if (!invItem.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} is not yet issued. Only currently issued PPE items can be transferred via PPEIR.");

            if (!registryByInventoryItemId.TryGetValue(itemId, out var registry))
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has no asset registry record. Create or reconcile its assignment before transfer.");

            if (!registry.CurrentCustodianId.HasValue)
            {
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} has no current custodian in the asset registry and cannot be transferred via PPEIR.");
            }
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        var ppeir = PPEIssuanceReport.Create(
            tenantId,
            command.PPEIRNo,
            command.Date,
            command.IssuedToEmployeeId,
            command.IssuedToOfficeAddress,
            command.IssuanceType,
            command.IssuedByEmployeeId,
            command.ReceivedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.DateReceived,
            command.DriverName,
            command.BillOfLadingNo);

        ppeir.CreatedBy = userId;
        _dbContext.PPEIssuanceReports.Add(ppeir);

        int itemNo = 1;
        foreach (var itemRequest in command.Items)
        {
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var ppeirItem = PPEIRItem.Create(
                tenantId,
                ppeir.Id,
                invItem.Id,
                itemNo,
                invItem.PropertyNo,
                null, // SerialNumber — not on TangibleInventoryItem snapshot
                invItem.Description ?? string.Empty,
                invItem.AcquisitionDate,
                invItem.UnitCost);

            _dbContext.PPEIRItems.Add(ppeirItem);

            invItem.MarkIssued();

            var registry = registryByInventoryItemId[invItem.Id];
            var previousCustodian = registry.CurrentCustodianId;
            registry.TransferOut();
            var eventType = previousCustodian.HasValue
                ? AssetAssignmentEventType.Transferred
                : AssetAssignmentEventType.Assigned;

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                eventType,
                DateTimeOffset.UtcNow,
                "PPEIR",
                ppeir.Id,
                ppeir.PPEIRNo,
                previousCustodian,
                command.IssuedToEmployeeId,
                null,
                null);

            _dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePPEIRResult(ppeir.Id, ppeir.PPEIRNo, command.Items.Count);
    }
}
