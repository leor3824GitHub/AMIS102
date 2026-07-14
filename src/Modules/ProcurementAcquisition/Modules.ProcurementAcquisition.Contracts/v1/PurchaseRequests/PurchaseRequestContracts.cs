using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum PurchaseRequestStatus
{
    Draft = 0,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    PendingFundsAvailable = 5,      // submitted by requester; awaiting Accountant
    PendingApproval = 6,            // certified by Accountant; awaiting HoPE
    ReturnedForRevision = 7,        // returned by Accountant or HoPE; back to Draft for edits
    Completed = 8                   // all resulting POs delivered & accepted (IAR) — procurement closed
}

public enum PrType
{
    Planned = 0,
    Unplanned = 1
}

/// <summary>
/// Whether a procurement document covers fixed/semi-expendable property (Asset) or consumable
/// supplies (Supply). Set once on the PR header and carried forward to PO and IAR. One PR is wholly
/// Asset or wholly Supply — never mixed. Drives the IAR acceptance rules and which integration event
/// fires on acceptance (AssetRegister vs. Expendable).
/// </summary>
public enum ProcurementCategory
{
    Asset = 0,
    Supply = 1
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
    string? UacsObjectCode = null,
    Guid? CatalogItemId = null,
    string? StockNumber = null);

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
    string? RejectionReason = null,
    // Signatory snapshots — frozen at the action so reprints stay faithful to who signed and their
    // title at the time. See faithful-reprint plan.
    Guid? RequestedById = null,
    string? RequestedByDesignation = null,
    string? ApprovedByDesignation = null,
    string? FundsAvailableCertifiedByDesignation = null,
    ProcurementCategory Category = ProcurementCategory.Asset);

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
    DateTimeOffset CreatedOnUtc,
    bool HasSignedCopy = false,
    ProcurementCategory Category = ProcurementCategory.Asset);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record CreatePurchaseRequestLineItemRequest(
    decimal Quantity,
    string UnitOfIssue,
    string ItemDescription,
    decimal EstimatedUnitCost,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null,
    string? StockNumber = null);

public sealed record CreatePurchaseRequestCommand(
    Guid DepartmentId,
    string? ResponsibilityCenterCode,
    string Purpose,
    PrType PrType,
    string? Justification,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems,
    ProcurementCategory Category = ProcurementCategory.Asset) : ICommand<PurchaseRequestDto>;

public sealed record UpdatePurchaseRequestCommand(
    Guid Id,
    Guid DepartmentId,
    string? ResponsibilityCenterCode,
    string Purpose,
    PrType PrType,
    string? Justification,
    string? SaiNumber,
    DateOnly? SaiDate,
    string? AlobsNumber,
    DateOnly? AlobsDate,
    IReadOnlyList<CreatePurchaseRequestLineItemRequest> LineItems,
    ProcurementCategory Category = ProcurementCategory.Asset) : ICommand<PurchaseRequestDto>;

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

    /// <summary>
    /// When <c>true</c>, excludes purchase requests whose every line item is already covered by a
    /// non-cancelled canvass request (RIV). PRs with at least one uncovered line remain eligible,
    /// since one PR may be split across multiple canvasses. Used by the "New Canvass Request" PR picker.
    /// </summary>
    public bool? ExcludeFullyCanvassed { get; set; }

    /// <summary>
    /// When <c>true</c>, excludes purchase requests whose goods have already been received — that is, one of the
    /// PR's purchase orders already carries a non-cancelled Inspection &amp; Acceptance Report. Used by the Job
    /// Order "Link Purchase Request" picker so a PR drops off once its procurement has been inspected and accepted.
    /// </summary>
    /// <remarks>
    /// IARs hang off a <c>PurchaseOrder</c> — never off a PR or a Job Order directly — so the check walks
    /// PR → PO → IAR. (A Job Order records its own inspection inline and never produces an IAR.)
    /// </remarks>
    public bool? ExcludeWithIar { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>One status tally returned by <see cref="GetPurchaseRequestStatusCountsQuery"/>.</summary>
public sealed record PurchaseRequestStatusCountDto(PurchaseRequestStatus Status, int Count);

/// <summary>
/// Returns the count of purchase requests per status in a single grouped query, optionally
/// scoped to a PR-date range. Backs the status-filter tab badges so the UI no longer fires
/// one search-count request per status.
/// </summary>
public sealed class GetPurchaseRequestStatusCountsQuery : IQuery<IReadOnlyList<PurchaseRequestStatusCountDto>>
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Integration Events
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Per-line UACS assignment captured at PR Funds Available certification.</summary>
public sealed record PurchaseRequestUacsCertifiedEventLine(int ItemNo, Guid CatalogItemId, string UacsObjectCode);

/// <summary>
/// Published when an Accountant certifies "Funds Available" on a PR. Carries the per-line UACS assignments
/// for any line item that references a <c>PropertyItemCatalog</c>. The AssetRegister module consumes this event
/// to back-fill UACS on Draft catalog rows and promote them to <c>Ready</c>.
/// </summary>
public sealed record PurchaseRequestUacsCertifiedEvent(
    Guid PurchaseRequestId,
    string PrNumber,
    IReadOnlyList<PurchaseRequestUacsCertifiedEventLine> Lines,
    string? TenantId,
    string CorrelationId = "") : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public string Source { get; } = "Procurement";
}

