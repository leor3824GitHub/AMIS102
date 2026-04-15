using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEItems.GetPPEItemList;

public sealed record GetPPEItemListQuery(
    string? Keyword,
    PPEItemStatus? Status,
    Guid? CurrentAccountableEmployeeId,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPPEItemResponse>;

public sealed record PagedPPEItemResponse(
    IReadOnlyList<PPEItemSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PPEItemSummaryDto(
    Guid Id,
    string PropertyCode,
    string PropertyNumber,
    string Description,
    string? SerialNumber,
    DateOnly DateAcquired,
    decimal UnitCost,
    string Status,
    Guid? CurrentAccountableEmployeeId);
