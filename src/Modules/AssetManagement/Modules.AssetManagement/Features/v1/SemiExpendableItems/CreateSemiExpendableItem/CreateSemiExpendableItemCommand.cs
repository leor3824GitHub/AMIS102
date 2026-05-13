using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;

public sealed record CreatePropertyItemCatalogCommand(
    string Code,
    string Name,
    string? Description = null,
    string? UACSObjectCode = null,
    string UnitOfMeasure = "Piece",
    int? EstimatedUsefulLifeYears = null) : ICommand<PropertyItemCatalogDto>;

public sealed record PropertyItemCatalogDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);

