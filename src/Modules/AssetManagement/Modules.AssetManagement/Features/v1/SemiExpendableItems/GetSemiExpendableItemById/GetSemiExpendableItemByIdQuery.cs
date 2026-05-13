using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;

public sealed record GetPropertyItemCatalogByIdQuery(Guid Id) : IQuery<PropertyItemCatalogDetailsDto>;

public sealed record PropertyItemCatalogDetailsDto(
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

