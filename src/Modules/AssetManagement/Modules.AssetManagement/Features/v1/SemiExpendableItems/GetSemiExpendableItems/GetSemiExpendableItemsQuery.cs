using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;

public sealed record GetPropertyItemCatalogQuery(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPropertyItemCatalogResponse>;

public sealed record PagedPropertyItemCatalogResponse(
    ICollection<PropertyItemCatalogSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PropertyItemCatalogSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);

