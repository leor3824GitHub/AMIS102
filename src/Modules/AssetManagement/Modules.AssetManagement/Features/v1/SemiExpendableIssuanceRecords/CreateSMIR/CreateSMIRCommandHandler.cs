using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableIssuanceRecords.CreateSMIR;

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
            .IgnoreQueryFilters()
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

        foreach (var itemId in invItemIds)
        {
            if (!invItems.TryGetValue(itemId, out var invItem))
                throw new NotFoundException($"TangibleInventoryItem with ID {itemId} not found.");

            if (invItem.IsIssued)
                throw new InvalidOperationException(
                    $"TangibleInventoryItem {invItem.PropertyNo} is currently issued and cannot be transferred via SMIR. " +
                    "The employee must first return it via RRSP.");
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
                smir.Id,
                invItem.Id,
                itemNo,
                itemRequest.Description ?? invItem.PropertyNo,
                invItem.UnitCost,
                invItem.AssetType);

            _dbContext.SMIRItems.Add(smirItem);

            // Mark the inventory item as issued — transferred out of the current tenant.
            invItem.MarkIssued();

            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateSMIRResult(smir.Id, smir.SMIRNo, invItemIds.Count);
    }
}
