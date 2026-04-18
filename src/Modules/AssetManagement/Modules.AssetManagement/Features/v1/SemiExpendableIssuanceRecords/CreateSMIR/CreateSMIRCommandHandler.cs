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

        var propertyIds = command.Items.Select(x => x.SemiExpendablePropertyId).Distinct().ToList();

        if (propertyIds.Count != command.Items.Count)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Items), "Duplicate property entries are not allowed in a single SMIR.")
            ]);
        }

        var properties = await _dbContext.SemiExpendableProperties
            .Where(x => propertyIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var propertyId in propertyIds)
        {
            if (!properties.TryGetValue(propertyId, out var prop))
            {
                throw new NotFoundException($"Semi-expendable property with ID {propertyId} not found.");
            }

            // Only OnHand or Returned properties can be transferred.
            // Issued = still under active ICS; supply officer must collect via RRSP first.
            if (prop.Status != PropertyStatus.OnHand && prop.Status != PropertyStatus.Returned)
            {
                throw new InvalidOperationException(
                    $"Property {prop.PropertyNo} has status '{prop.Status}' and cannot be issued via SMIR. " +
                    $"Only OnHand or Returned properties can be transferred. " +
                    $"If the property is Issued, the employee must first return it via RRSP.");
            }
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
            var property = properties[itemRequest.SemiExpendablePropertyId];

            var smirItem = SMIRItem.Create(
                smir.Id,
                property.Id,
                itemNo,
                itemRequest.Description ?? property.PropertyNo,
                property.UnitCost,
                property.Category);

            _dbContext.SMIRItems.Add(smirItem);

            // Mark as Transferred — removed from active inventory, audit trail preserved.
            property.SetStatus(PropertyStatus.Transferred, custodianId: null);
            property.LastModifiedBy = userId;

            itemNo++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateSMIRResult(smir.Id, smir.SMIRNo, propertyIds.Count);
    }
}
