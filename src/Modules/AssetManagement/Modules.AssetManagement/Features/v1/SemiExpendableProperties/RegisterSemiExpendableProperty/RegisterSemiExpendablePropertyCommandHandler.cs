using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.RegisterSemiExpendableProperty;

public sealed class RegisterSemiExpendablePropertyCommandHandler : ICommandHandler<RegisterSemiExpendablePropertyCommand, SemiExpendablePropertyDto>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RegisterSemiExpendablePropertyCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SemiExpendablePropertyDto> Handle(RegisterSemiExpendablePropertyCommand command, CancellationToken cancellationToken)
    {
        var propertyNoInUse = await _dbContext.SemiExpendableProperties
            .IgnoreQueryFilters()
            .AnyAsync(x => x.PropertyNo == command.PropertyNo, cancellationToken)
            .ConfigureAwait(false);

        if (propertyNoInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.PropertyNo), "A property with this PropertyNo already exists.")
            ]);
        }

        var item = await _dbContext.SemiExpendableItems
            .FirstOrDefaultAsync(x => x.Id == command.SemiExpendableItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Semi-expendable item with ID {command.SemiExpendableItemId} not found.");
        }

        var property = SemiExpendableProperty.Create(
            command.PropertyNo,
            command.SemiExpendableItemId,
            command.Category,
            command.SerialNo,
            command.AcquisitionDate,
            command.UnitCost,
            command.FundCluster,
            command.Remarks);

        property.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.SemiExpendableProperties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SemiExpendablePropertyDto(
            property.Id,
            property.PropertyNo,
            property.SemiExpendableItemId,
            item.Code,
            item.Name,
            property.Category.ToString(),
            property.SerialNo,
            property.AcquisitionDate,
            property.UnitCost,
            property.FundCluster,
            property.Status.ToString(),
            property.CurrentCustodianId,
            property.Remarks);
    }
}
