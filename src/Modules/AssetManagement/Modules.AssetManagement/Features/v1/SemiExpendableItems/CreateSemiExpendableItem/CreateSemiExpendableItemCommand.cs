using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;

public sealed record CreateSemiExpendableItemCommand(
    string Code,
    string Name,
    string? Description = null,
    string? UACSObjectCode = null,
    string UnitOfMeasure = "Piece",
    int? EstimatedUsefulLifeYears = null) : ICommand<SemiExpendableItemDto>;

public sealed record SemiExpendableItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);
