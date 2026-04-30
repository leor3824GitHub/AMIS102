using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.CreatePAR;

public sealed class CreatePARCommandHandler : ICommandHandler<CreatePARCommand, CreatePARResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePARCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreatePARResult> Handle(CreatePARCommand command, CancellationToken cancellationToken)
    {
        var parNoInUse = await _dbContext.PropertyAcknowledgementReceipts
            .AnyAsync(x => x.PARNo == command.PARNo, cancellationToken)
            .ConfigureAwait(false);

        if (parNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PARNo), "A PAR with this number already exists.")
            ]);
        }

        var requestedItemIds = command.Items.Select(x => x.TangibleInventoryItemId).Distinct().ToList();

        if (requestedItemIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate inventory item entries are not allowed in a single PAR.")
            ]);
        }

        var invItems = await (
            from inv in _dbContext.TangibleInventoryItems.Where(x => requestedItemIds.Contains(x.Id))
            join catalog in _dbContext.PropertyItemCatalog on inv.ItemId equals catalog.Id
            select new { Inv = inv, EUL = catalog.EstimatedUsefulLifeYears })
            .ToDictionaryAsync(x => x.Inv.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedItemIds)
        {
            if (!invItems.TryGetValue(itemId, out var row))
                throw new KeyNotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (row.Inv.AssetType != AssetType.PPE)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {row.Inv.PropertyNo} has AssetType '{row.Inv.AssetType}'. Only PPE items can be assigned via PAR.");

            if (row.Inv.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {row.Inv.PropertyNo} is already issued.");
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;

        var par = PropertyAcknowledgementReceipt.Create(
            tenantId,
            command.PARNo,
            command.Date,
            command.PARType,
            command.ReceivedFromEmployeeId,
            command.ReceivedByEmployeeId,
            command.ApprovedByEmployeeId);

        par.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.PropertyAcknowledgementReceipts.Add(par);

        int itemNo = 1;
        foreach (var itemRequest in command.Items)
        {
            var row = invItems[itemRequest.TangibleInventoryItemId];

            var parItem = PARItem.Create(
                tenantId,
                par.Id,
                row.Inv.Id,
                itemNo,
                itemRequest.Quantity,
                itemRequest.Unit,
                itemRequest.ItemDescription,
                row.Inv.UnitCost,
                row.EUL ?? 0,
                row.Inv.AcquisitionDate);

            _dbContext.PARItems.Add(parItem);
            row.Inv.MarkIssued();
            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreatePARResult(par.Id, par.PARNo, command.Items.Count);
    }
}
