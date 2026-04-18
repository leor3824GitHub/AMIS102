using FSH.Framework.Core.Context;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;

public sealed class CreatePropertyItemCatalogCommandHandler : ICommandHandler<CreatePropertyItemCatalogCommand, PropertyItemCatalogDto>
{
    private readonly AssetManagementDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreatePropertyItemCatalogCommandHandler(AssetManagementDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<PropertyItemCatalogDto> Handle(CreatePropertyItemCatalogCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.PropertyItemCatalog
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "An item with this code already exists in the catalog.")
            ]);
        }

        string tenantId = _currentUser.GetTenant() ?? string.Empty;

        var item = PropertyItemCatalog.Create(
            tenantId,
            command.Code,
            command.Name,
            command.Description,
            command.UACSObjectCode,
            command.UnitOfMeasure,
            command.EstimatedUsefulLifeYears);

        item.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.PropertyItemCatalog.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PropertyItemCatalogDto(
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
