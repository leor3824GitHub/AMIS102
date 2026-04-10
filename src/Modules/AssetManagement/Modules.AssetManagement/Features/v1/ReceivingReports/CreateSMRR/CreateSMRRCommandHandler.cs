using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;

public sealed class CreateSMRRCommandHandler : ICommandHandler<CreateSMRRCommand, CreateSMRRResult>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateSMRRCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<CreateSMRRResult> Handle(CreateSMRRCommand command, CancellationToken cancellationToken)
    {
        var smrrNoInUse = await _dbContext.SuppliesMaterialsReceivingReports
            .IgnoreQueryFilters()
            .AnyAsync(x => x.SMRRNo == command.SMRRNo, cancellationToken)
            .ConfigureAwait(false);

        if (smrrNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.SMRRNo), "A receiving report with this SMRR number already exists.")
            ]);
        }

        var requestedItemIds = command.Items.Select(x => x.SemiExpendableItemId).Distinct().ToList();
        var catalogItems = await _dbContext.SemiExpendableItems
            .Where(x => requestedItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var itemId in requestedItemIds)
        {
            if (!catalogItems.ContainsKey(itemId))
            {
                throw new KeyNotFoundException($"Semi-expendable item with ID {itemId} not found.");
            }
        }

        var smrr = SuppliesMaterialsReceivingReport.Create(
            command.SMRRNo,
            command.Date,
            command.ReceivedFrom,
            command.Address,
            command.ReceiptType,
            command.OtherReceiptType,
            command.FundCluster,
            command.ReceivedBy,
            command.NotedBy);

        smrr.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.SuppliesMaterialsReceivingReports.Add(smrr);

        int propertiesCreated = 0;
        int year = command.Date.Year;
        int sequence = await GetNextPropertySequenceAsync(year, cancellationToken).ConfigureAwait(false);

        foreach (var itemRequest in command.Items)
        {
            var smrrItem = SMRRItem.Create(
                smrr.Id,
                itemRequest.Reference,
                itemRequest.SemiExpendableItemId,
                itemRequest.Description,
                itemRequest.AcquisitionDate,
                itemRequest.Quantity,
                itemRequest.UnitCost);

            _dbContext.SMRRItems.Add(smrrItem);

            var catalogItem = catalogItems[itemRequest.SemiExpendableItemId];

            for (int i = 0; i < itemRequest.Quantity; i++)
            {
                var propertyNo = $"AM-{year}-{sequence:D5}";
                sequence++;

                var property = SemiExpendableProperty.Create(
                    propertyNo,
                    itemRequest.SemiExpendableItemId,
                    serialNo: null,
                    itemRequest.AcquisitionDate,
                    itemRequest.UnitCost,
                    command.FundCluster,
                    remarks: null,
                    smrrItemId: smrrItem.Id);

                property.CreatedBy = _currentUser.GetUserId().ToString();
                _dbContext.SemiExpendableProperties.Add(property);
                propertiesCreated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateSMRRResult(smrr.Id, smrr.SMRRNo, propertiesCreated);
    }

    private async Task<int> GetNextPropertySequenceAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"AM-{year}-";
        var lastNo = await _dbContext.SemiExpendableProperties
            .IgnoreQueryFilters()
            .Where(x => x.PropertyNo.StartsWith(prefix))
            .OrderByDescending(x => x.PropertyNo)
            .Select(x => x.PropertyNo)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lastNo is null)
        {
            return 1;
        }

        var sequencePart = lastNo[prefix.Length..];
        return int.TryParse(sequencePart, out var seq) ? seq + 1 : 1;
    }
}
