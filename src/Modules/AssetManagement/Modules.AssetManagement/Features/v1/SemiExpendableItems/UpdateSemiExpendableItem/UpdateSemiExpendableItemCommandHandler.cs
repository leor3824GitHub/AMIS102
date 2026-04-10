using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;

public sealed class UpdateSemiExpendableItemCommandHandler : ICommandHandler<UpdateSemiExpendableItemCommand, Unit>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateSemiExpendableItemCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateSemiExpendableItemCommand command, CancellationToken cancellationToken)
    {
        var item = await _dbContext.SemiExpendableItems
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Semi-expendable item with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.SemiExpendableItems
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A semi-expendable item with this code already exists.")
            ]);
        }

        item.Update(
            command.Code,
            command.Name,
            command.Description,
            command.UACSObjectCode,
            command.UnitOfMeasure,
            command.EstimatedUsefulLifeYears,
            command.IsActive);

        item.LastModifiedBy = _currentUser.GetUserId().ToString();

        _dbContext.SemiExpendableItems.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
