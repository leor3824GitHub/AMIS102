using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;

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
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ICSNo == command.ICSNo, cancellationToken)
            .ConfigureAwait(false);

        if (icsNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.ICSNo), "An ICS with this number already exists.")
            ]);
        }

        var propertyIds = command.Items.Select(x => x.SemiExpendablePropertyId).Distinct().ToList();

        if (propertyIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate property entries are not allowed in a single ICS.")
            ]);
        }

        var properties = await _dbContext.SemiExpendableProperties
            .Include(x => x.SemiExpendableItem)
            .Where(x => propertyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var propertyId in propertyIds)
        {
            if (!properties.TryGetValue(propertyId, out var prop))
            {
                throw new KeyNotFoundException($"Semi-expendable property with ID {propertyId} not found.");
            }

            if (prop.Status != PropertyStatus.OnHand)
            {
                throw new InvalidOperationException(
                    $"Property {prop.PropertyNo} has status '{prop.Status}' and cannot be issued. Only OnHand properties can be issued.");
            }
        }

        var ics = InventoryCustodianSlip.Create(
            command.ICSNo,
            command.Date,
            command.FundCluster,
            command.IssuedFromEmployeeId,
            command.ReceivedByEmployeeId);

        ics.CreatedBy = _currentUser.GetUserId().ToString();
        _dbContext.InventoryCustodianSlips.Add(ics);

        int itemNo = 1;
        foreach (var itemRequest in command.Items)
        {
            var property = properties[itemRequest.SemiExpendablePropertyId];

            var icsItem = ICSItem.Create(
                ics.Id,
                property.Id,
                itemNo,
                itemRequest.Description ?? property.SemiExpendableItem?.Name,
                property.UnitCost,
                property.SemiExpendableItem?.EstimatedUsefulLifeYears);

            _dbContext.ICSItems.Add(icsItem);
            property.SetStatus(PropertyStatus.Issued, command.ReceivedByEmployeeId);
            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateICSResult(ics.Id, ics.ICSNo, command.Items.Count);
    }
}
