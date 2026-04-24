using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItems;

public sealed record GetTangibleItemsQuery(
    string? Keyword = null,
    string? PropertyClass = null,
    string? CategoryCode = null,
    bool? ExcludeLinked = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedTangibleItemsResponse>;

public sealed record PagedTangibleItemsResponse(
    ICollection<TangibleItemSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record TangibleItemSummaryDto(
    Guid Id,
    string PropertyNo,
    string PropertyClass,
    string CategoryCode,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks);
