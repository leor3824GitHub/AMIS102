using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum AssetPurchaseRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum AssetPrType
{
    Planned = 0,
    Unplanned = 1
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetPurchaseRequestLineItemDto(
    int ItemNo,
    string ItemDescription,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal EstimatedUnitCost,
    decimal EstimatedTotalCost);

public sealed record AssetPurchaseRequestDto(
    Guid Id,
    string PrNumber,
    DateOnly PrDate,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    Guid DepartmentId,
    string DepartmentName,
    string? Section,
    string Purpose,
    AssetPrType PrType,
    string? Justification,
    AssetPurchaseRequestStatus Status,
    Guid RequestedById,
    string RequestedByName,
    Guid? ApprovedById,
    string? ApprovedByName,
    string? RejectionReason,
    IReadOnlyList<AssetPurchaseRequestLineItemDto> LineItems,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record AssetPurchaseRequestSummaryDto(
    Guid Id,
    string PrNumber,
    DateOnly PrDate,
    string DepartmentName,
    string? Section,
    string Purpose,
    AssetPrType PrType,
    AssetPurchaseRequestStatus Status,
    int LineItemCount,
    decimal TotalEstimatedCost,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetPurchaseRequestLineItemRequest(
    string ItemDescription,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal EstimatedUnitCost);

public sealed record CreateAssetPurchaseRequestCommand(
    Guid DepartmentId,
    string? Section,
    string Purpose,
    AssetPrType PrType,
    string? Justification,
    Guid RequestedById,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<AssetPurchaseRequestLineItemRequest> LineItems) : ICommand<AssetPurchaseRequestDto>;

public sealed record UpdateAssetPurchaseRequestCommand(
    Guid Id,
    Guid DepartmentId,
    string? Section,
    string Purpose,
    AssetPrType PrType,
    string? Justification,
    Guid RequestedById,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<AssetPurchaseRequestLineItemRequest> LineItems) : ICommand<AssetPurchaseRequestDto>;

public sealed record SubmitAssetPurchaseRequestCommand(Guid Id) : ICommand<AssetPurchaseRequestDto>;

public sealed record ApproveAssetPurchaseRequestCommand(Guid Id, Guid ApprovedById) : ICommand<AssetPurchaseRequestDto>;

public sealed record RejectAssetPurchaseRequestCommand(Guid Id, string Reason) : ICommand<AssetPurchaseRequestDto>;

public sealed record CancelAssetPurchaseRequestCommand(Guid Id, string? Reason = null) : ICommand<AssetPurchaseRequestDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetAssetPurchaseRequestQuery(Guid Id) : IQuery<AssetPurchaseRequestDto?>;

public sealed class SearchAssetPurchaseRequestsQuery : IQuery<PagedResponse<AssetPurchaseRequestSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? DepartmentId { get; set; }
    public AssetPurchaseRequestStatus? Status { get; set; }
    public AssetPrType? PrType { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

