using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventories;

public sealed record GetTangibleInventoriesQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReceiptType? ReceiptType,
    AssetType? AssetType,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedTangibleInventoriesResponse>;

public sealed record PagedTangibleInventoriesResponse(
    IReadOnlyList<TangibleInventorySummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record TangibleInventorySummaryDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string ReceiptType,
    string? FundCluster,
    int SEItemCount,
    int PPEItemCount);
