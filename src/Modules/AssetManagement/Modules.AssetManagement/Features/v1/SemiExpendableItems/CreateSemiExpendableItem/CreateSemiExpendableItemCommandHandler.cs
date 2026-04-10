using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;

public sealed class CreateSemiExpendableItemCommandHandler : ICommandHandler<CreateSemiExpendableItemCommand, SemiExpendableItemDto>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateSemiExpendableItemCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<SemiExpendableItemDto> Handle(CreateSemiExpendableItemCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.SemiExpendableItems
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A semi-expendable item with this code already exists.")
            ]);
        }

        var item = SemiExpendableItem.Create(
            command.Code,
            command.Name,
            command.Description,
            command.UACSObjectCode,
            command.UnitOfMeasure,
            command.EstimatedUsefulLifeYears);

        item.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.SemiExpendableItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SemiExpendableItemDto(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.UACSObjectCode,
            item.UnitOfMeasure,
            item.EstimatedUsefulLifeYears,
            item.IsActive);
    }
}
