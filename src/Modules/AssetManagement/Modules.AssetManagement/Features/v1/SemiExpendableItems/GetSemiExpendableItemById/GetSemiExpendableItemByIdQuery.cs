using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public sealed record GetSemiExpendableItemByIdQuery(Guid Id) : IQuery<SemiExpendableItemDetailsDto>;

public sealed record SemiExpendableItemDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);
