using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum PurchaseRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum PrType
{
    Planned = 0,
    Unplanned = 1
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record PurchaseRequestLineItemDto(
    int ItemNo,
    decimal Quantity,
    string UnitOfIssue,
    string ItemDescription,
    decimal EstimatedUnitCost,
    decimal EstimatedTotalCost);

public sealed record PurchaseRequestDto(
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
    PrType PrType,
    string? Justification,
    PurchaseRequestStatus Status,
    Guid RequestedById,
    string RequestedByName,
    Guid? ApprovedById,
    string? ApprovedByName,
    IReadOnlyList<PurchaseRequestLineItemDto> LineItems,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record PurchaseRequestSummaryDto(
    Guid Id,
    string PrNumber,
    DateOnly PrDate,
    string DepartmentName,
    string? Section,
    string Purpose,
    PrType PrType,
    PurchaseRequestStatus Status,
    int LineItemCount,
    decimal TotalEstimatedCost,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record CreatePurchaseRequestLineItemRequest(
    decimal Quantity,
    string UnitOfIssue,
    string ItemDescription,
    decimal EstimatedUnitCost);

public sealed record CreatePurchaseRequestCommand(
    Guid DepartmentId,
    string? Section,
    string Purpose,
    PrType PrType,
    string? Justification,
    Guid RequestedById,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems) : ICommand<PurchaseRequestDto>;

public sealed record UpdatePurchaseRequestCommand(
    Guid Id,
    Guid DepartmentId,
    string? Section,
    string Purpose,
    PrType PrType,
    string? Justification,
    Guid RequestedById,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems) : ICommand<PurchaseRequestDto>;

public sealed record SubmitPurchaseRequestCommand(Guid Id) : ICommand<PurchaseRequestDto>;

public sealed record ApprovePurchaseRequestCommand(Guid Id, Guid ApprovedById) : ICommand<PurchaseRequestDto>;

public sealed record RejectPurchaseRequestCommand(Guid Id, string Reason) : ICommand<PurchaseRequestDto>;

public sealed record CancelPurchaseRequestCommand(Guid Id, string? Reason = null) : ICommand<PurchaseRequestDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetPurchaseRequestQuery(Guid Id) : IQuery<PurchaseRequestDto?>;

public sealed class SearchPurchaseRequestsQuery : IQuery<PagedResponse<PurchaseRequestSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? DepartmentId { get; set; }
    public PurchaseRequestStatus? Status { get; set; }
    public PrType? PrType { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
