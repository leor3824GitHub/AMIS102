using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;

public sealed record UpdateSemiExpendableItemCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    string? UACSObjectCode = null,
    string UnitOfMeasure = "Piece",
    int? EstimatedUsefulLifeYears = null,
    bool IsActive = true) : ICommand;
