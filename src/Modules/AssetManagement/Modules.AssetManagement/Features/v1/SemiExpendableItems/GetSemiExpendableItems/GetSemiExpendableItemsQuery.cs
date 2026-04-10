using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;

public sealed record GetSemiExpendableItemsQuery(
    string? Keyword = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedSemiExpendableItemsResponse>;

public sealed record PagedSemiExpendableItemsResponse(
    ICollection<SemiExpendableItemSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record SemiExpendableItemSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);
