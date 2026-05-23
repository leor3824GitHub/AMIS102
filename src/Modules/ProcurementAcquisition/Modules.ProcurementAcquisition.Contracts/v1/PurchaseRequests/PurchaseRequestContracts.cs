using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum PurchaseRequestStatus
{
    Draft = 0,
    Submitted = 1,                  // legacy single-step submissions; kept for backward compat
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    PendingFundsAvailable = 5,      // submitted by requester; awaiting Accountant
    PendingApproval = 6,            // certified by Accountant; awaiting HoPE
    ReturnedForRevision = 7         // returned by Accountant or HoPE; back to Draft for edits
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
    decimal EstimatedTotalCost,
    string? UacsObjectCode = null);

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
    string? ResponsibilityCenterCode,
    string Purpose,
    PrType PrType,
    string? Justification,
    PurchaseRequestStatus Status,
    string RequestedByName,
    string? ApprovedByName,
    IReadOnlyList<PurchaseRequestLineItemDto> LineItems,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    Guid? FundsAvailableCertifiedById = null,
    string? FundsAvailableCertifiedByName = null,
    DateTimeOffset? FundsAvailableCertifiedOnUtc = null,
    Guid? ApprovedById = null,
    DateTimeOffset? ApprovedOnUtc = null,
    string? ReturnedReason = null,
    Guid? ReturnedById = null,
    string? ReturnedByName = null,
    DateTimeOffset? ReturnedOnUtc = null,
    string? RejectionReason = null);

public sealed record PurchaseRequestSummaryDto(
    Guid Id,
    string PrNumber,
    DateOnly PrDate,
    string DepartmentName,
    string? ResponsibilityCenterCode,
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
    string? ResponsibilityCenterCode,
    string Purpose,
    PrType PrType,
    string? Justification,
    string RequestedByName,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems) : ICommand<PurchaseRequestDto>;

public sealed record UpdatePurchaseRequestCommand(
    Guid Id,
    Guid DepartmentId,
    string? ResponsibilityCenterCode,
    string Purpose,
    PrType PrType,
    string? Justification,
    string RequestedByName,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems) : ICommand<PurchaseRequestDto>;

public sealed record SubmitPurchaseRequestCommand(Guid Id) : ICommand<PurchaseRequestDto>;

public sealed record ApprovePurchaseRequestCommand(Guid Id, string ApprovedByName) : ICommand<PurchaseRequestDto>;

/// <summary>Per-line UACS assignment supplied by the Accountant during Funds Available certification.</summary>
public sealed record LineUacsAssignment(int ItemNo, string UacsObjectCode);

/// <summary>
/// Accountant signs the "Funds Available" portion of the PR form. Assigns a UACS Object Code per line item
/// and (optionally) captures the ALOBS reference. Moves PR from PendingFundsAvailable to PendingApproval.
/// </summary>
public sealed record CertifyFundsAvailableCommand(
    Guid Id,
    string CertifiedByName,
    IReadOnlyList<LineUacsAssignment> UacsByLine,
    string? AlobsNumber = null,
    DateOnly? AlobsDate = null) : ICommand<PurchaseRequestDto>;

/// <summary>Either approver (Accountant or HoPE) returns the PR for revision with a reason. Reverts to Draft.</summary>
public sealed record ReturnPurchaseRequestForRevisionCommand(
    Guid Id,
    string ReturnedByName,
    string Reason) : ICommand<PurchaseRequestDto>;

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

