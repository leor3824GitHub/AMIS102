using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.RenewICS;

public sealed class RenewICSCommandHandler : ICommandHandler<RenewICSCommand, RenewICSResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RenewICSCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<RenewICSResult> Handle(RenewICSCommand command, CancellationToken cancellationToken)
    {
        var oldIcs = await _dbContext.InventoryCustodianSlips
            .FirstOrDefaultAsync(x => x.Id == command.OldICSId, cancellationToken)
            .ConfigureAwait(false);

        if (oldIcs is null)
        {
            throw new NotFoundException($"ICS with ID {command.OldICSId} not found.");
        }

        if (oldIcs.Status != ICSStatus.Active)
        {
            throw new InvalidOperationException(
                $"ICS {oldIcs.ICSNo} has status '{oldIcs.Status}' and cannot be renewed. Only Active ICS records can be renewed.");
        }

        var newIcsNoInUse = await _dbContext.InventoryCustodianSlips
            .AnyAsync(x => x.ICSNo == command.NewICSNo, cancellationToken)
            .ConfigureAwait(false);

        if (newIcsNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.NewICSNo), "An ICS with this number already exists.")
            ]);
        }

        // Load the items belonging to the old ICS.
        var oldItems = await _dbContext.ICSItems
            .Where(x => x.ICSId == command.OldICSId)
            .OrderBy(x => x.ItemNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var tangibleInventoryItemIds = oldItems.Select(x => x.TangibleInventoryItemId).Distinct().ToList();
        var registryByInventoryItemId = await _dbContext.AssetRegistry
            .Where(x => tangibleInventoryItemIds.Contains(x.TangibleInventoryItemId))
            .ToDictionaryAsync(x => x.TangibleInventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        string tenantId = _currentUser.GetTenant() ?? string.Empty;
        string userId = _currentUser.GetUserId().ToString();

        var newIcs = InventoryCustodianSlip.Create(
            tenantId,
            command.NewICSNo,
            command.Date,
            oldIcs.Category,
            oldIcs.FundCluster,
            command.IssuedFromEmployeeId,
            oldIcs.ReceivedByEmployeeId,
            renewedFromICSId: oldIcs.Id);

        newIcs.CreatedBy = userId;
        _dbContext.InventoryCustodianSlips.Add(newIcs);

        // Copy items from old ICS to the new one, freezing asset type at renewal date.
        int itemNo = 1;
        foreach (var oldItem in oldItems)
        {
            var newItem = ICSItem.Create(
                tenantId,
                newIcs.Id,
                oldItem.TangibleInventoryItemId,
                itemNo,
                oldItem.Description,
                oldItem.UnitCost,
                oldItem.EstimatedUsefulLifeYears,
                oldItem.AssetTypeAtTimeOfIssuance);

            _dbContext.ICSItems.Add(newItem);
            itemNo++;
        }

        // Mark the old ICS as renewed and cross-link it to the new one.
        oldIcs.MarkRenewed(newIcs.Id);
        oldIcs.LastModifiedBy = userId;

        foreach (var oldItem in oldItems)
        {
            if (!registryByInventoryItemId.TryGetValue(oldItem.TangibleInventoryItemId, out var registry))
            {
                continue;
            }

            var history = AssetAssignmentHistory.Create(
                tenantId,
                registry.Id,
                AssetAssignmentEventType.StatusChanged,
                DateTimeOffset.UtcNow,
                "ICS-RENEWAL",
                newIcs.Id,
                newIcs.ICSNo,
                registry.CurrentCustodianId,
                registry.CurrentCustodianId,
                registry.CurrentLocationId,
                $"ICS renewed from {oldIcs.ICSNo} to {newIcs.ICSNo}.");

            _dbContext.AssetAssignmentHistory.Add(history);
            registry.LinkCurrentAssignment(history.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RenewICSResult(newIcs.Id, newIcs.ICSNo, oldIcs.Id, oldItems.Count);
    }
}
