using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.CreateRRP;

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
            .IgnoreQueryFilters()
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

        var rrp = ReceiptForReturnedPPE.Create(
            tenantId,
            command.RRPNo,
            command.Date,
            command.ReturnCategory,
            command.ReturnedByEmployeeId,
            command.ApprovedByEmployeeId,
            command.SignedByEmployeeId,
            command.PropertyInspectorCertified);

        rrp.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.ReceiptsForReturnedPPE.Add(rrp);

        foreach (var (itemRequest, itemNo) in command.Items.Select((r, i) => (r, i + 1)))
        {
            var invItem = invItems[itemRequest.TangibleInventoryItemId];

            var rrpItem = RRPItem.Create(
                rrp.Id,
                invItem.Id,
                itemNo,
                itemRequest.SourceDocumentRef,
                invItem.PropertyNo,
                invItem.Description ?? string.Empty,
                itemRequest.Quantity,
                invItem.UnitCost);

            _dbContext.RRPItems.Add(rrpItem);

            if (command.ReturnCategory == PPEReturnCategory.Serviceable)
            {
                invItem.MarkReturned();
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateRRPResult(rrp.Id, rrp.RRPNo, command.Items.Count);
    }
}
