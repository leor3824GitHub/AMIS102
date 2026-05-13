using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.CreateTangibleInventory;

public sealed class CreateTangibleInventoryCommandHandler(
    AssetManagementDbContext dbContext,
    IMediator mediator,
    ICurrentUser currentUser) : ICommandHandler<CreateTangibleInventoryCommand, CreateTangibleInventoryResult>
{
    public async ValueTask<CreateTangibleInventoryResult> Handle(
        CreateTangibleInventoryCommand command,
        CancellationToken cancellationToken)
    {
        // Validate unique ReportNo
        var reportNoInUse = await dbContext.TangibleInventories
            .AnyAsync(x => x.ReportNo == command.ReportNo, cancellationToken)
            .ConfigureAwait(false);

        if (reportNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(command.ReportNo),
                    "A tangible inventory report with this number already exists.")
            ]);
        }

        // Fetch the active capitalization threshold (cross-module via Mediator)
        var threshold = await mediator
            .Send(new GetActiveCapitalizationThresholdQuery(), cancellationToken)
            .ConfigureAwait(false);

        if (threshold is null)
        {
            throw new InvalidOperationException(
                "No active capitalization threshold is configured. " +
                "Please set an active threshold in Master Data before creating a Tangible Inventory report.");
        }

        // Load all referenced tangible items with their item catalog entries
        var tangibleItemIds = command.Items.Select(x => x.TangibleItemId).Distinct().ToList();
        var tangibleItems = await dbContext.TangibleItems
            .Include(x => x.Item)
            .Where(x => tangibleItemIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Validate each line item
        foreach (var itemRequest in command.Items)
        {
            var ti = tangibleItems.FirstOrDefault(x => x.Id == itemRequest.TangibleItemId);

            if (ti is null)
            {
                throw new KeyNotFoundException(
                    $"Tangible item with ID {itemRequest.TangibleItemId} not found.");
            }

            if (ti.TangibleInventoryItemId.HasValue)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(
                        nameof(itemRequest.TangibleItemId),
                        $"Tangible item {ti.PropertyNo} is already linked to a receiving report.")
                ]);
            }
        }

        string tenantId = currentUser.GetTenant() ?? string.Empty;
        string userId = currentUser.GetUserId().ToString();

        var inventory = Domain.TangibleInventory.Create(
            tenantId,
            command.ReportNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptType,
            command.OtherReceiptType,
            command.FundCluster,
            command.ReceivedByEmployeeId,
            command.NotedByEmployeeId);

        inventory.CreatedBy = userId;
        dbContext.TangibleInventories.Add(inventory);

        int seCount = 0, ppeCount = 0;

        // Create TangibleInventoryItems (snapshot from TangibleItem) and back-link each item
        foreach (var itemRequest in command.Items)
        {
            var ti = tangibleItems.First(x => x.Id == itemRequest.TangibleItemId);

            // Determine SE vs PPE at receipt time — snapshotted on the item
            var assetType = ti.UnitCost >= threshold.CapitalizationAmount
                ? AssetType.PPE
                : AssetType.SE;

            var inventoryItem = TangibleInventoryItem.Create(
                tenantId,
                inventory.Id,
                ti.Id,
                itemRequest.Reference,
                assetType,
                threshold.CapitalizationAmount,
                ti.PropertyNo,
                ti.ItemId,
                ti.Item?.Name,
                ti.AcquisitionDate,
                ti.Quantity,
                ti.UnitCost);

            dbContext.TangibleInventoryItems.Add(inventoryItem);
            ti.LinkToInventory(inventoryItem.Id);

            var registry = AssetRegistry.Create(
                tenantId,
                inventoryItem.Id,
                ti.ItemId,
                ti.PropertyNo,
                assetType,
                ti.AcquisitionDate,
                ti.UnitCost);

            registry.CreatedBy = userId;
            dbContext.AssetRegistry.Add(registry);

            if (assetType == AssetType.SE) seCount++;
            else ppeCount++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateTangibleInventoryResult(inventory.Id, inventory.ReportNo, seCount, ppeCount);
    }
}

