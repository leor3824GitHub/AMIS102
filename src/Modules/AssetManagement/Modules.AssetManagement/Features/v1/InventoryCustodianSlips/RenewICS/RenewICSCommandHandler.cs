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

        // Validate the new ICS number prefix matches the category being renewed.
        var expectedPrefix = oldIcs.Category == AssetCategory.LowValuedSemi ? "SPLV-" : "SPHV-";
        if (!command.NewICSNo.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(command.NewICSNo),
                    $"New ICS number must start with '{expectedPrefix}' to match the category '{oldIcs.Category}' of the ICS being renewed.")
            ]);
        }

        var newIcsNoInUse = await _dbContext.InventoryCustodianSlips
            .IgnoreQueryFilters()
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

        string userId = _currentUser.GetUserId().ToString();

        var newIcs = InventoryCustodianSlip.Create(
            command.NewICSNo,
            command.Date,
            oldIcs.Category,
            oldIcs.FundCluster,
            command.IssuedFromEmployeeId,
            oldIcs.ReceivedByEmployeeId,
            renewedFromICSId: oldIcs.Id);

        newIcs.CreatedBy = userId;
        _dbContext.InventoryCustodianSlips.Add(newIcs);

        // Copy items from old ICS to the new one, freezing category at renewal date.
        int itemNo = 1;
        foreach (var oldItem in oldItems)
        {
            var newItem = ICSItem.Create(
                newIcs.Id,
                oldItem.SemiExpendablePropertyId,
                itemNo,
                oldItem.Description,
                oldItem.UnitCost,
                oldItem.EstimatedUsefulLifeYears,
                oldItem.CategoryAtTimeOfIssuance);

            _dbContext.ICSItems.Add(newItem);
            itemNo++;
        }

        // Mark the old ICS as renewed and cross-link it to the new one.
        oldIcs.MarkRenewed(newIcs.Id);
        oldIcs.LastModifiedBy = userId;

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RenewICSResult(newIcs.Id, newIcs.ICSNo, oldIcs.Id, oldItems.Count);
    }
}
