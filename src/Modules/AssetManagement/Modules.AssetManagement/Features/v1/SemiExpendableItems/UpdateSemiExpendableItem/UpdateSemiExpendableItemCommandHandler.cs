using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;

public sealed class UpdatePropertyItemCatalogCommandHandler : ICommandHandler<UpdatePropertyItemCatalogCommand, Unit>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdatePropertyItemCatalogCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdatePropertyItemCatalogCommand command, CancellationToken cancellationToken)
    {
        var item = await _dbContext.PropertyItemCatalog
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Item catalog entry with ID {command.Id} not found.");
        }

        var codeInUse = await _dbContext.PropertyItemCatalog
            .AnyAsync(x => x.Code == command.Code && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "An item with this code already exists in the catalog.")
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

        _dbContext.PropertyItemCatalog.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

