using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Lifecycle of a Job Order (works: renovation / repair / fabrication). Mirrors the PO workflow through
/// issuance, then captures inspection &amp; acceptance on the JO itself — works are not inventoried, so a JO
/// never feeds an Inspection &amp; Acceptance Report or the Asset Register / Expendable pipeline.
/// </summary>
public enum JobOrderStatus
{
    Draft = 0,
    PendingFundsAvailable = 1,  // submitted by buyer; awaiting Accountant funds-available certification
    PendingApproval = 2,        // certified by Accountant; awaiting Issue (HoPE/Buyer)
    Issued = 3,                 // issued to supplier; work to be performed
    Inspected = 4,              // work inspected by the C.O./F.O. Inspector
    Completed = 5,              // work accepted by the Supply Officer
    Cancelled = 6
}

// NOTE: ModeOfProcurement is shared with PurchaseOrders (same procurement modes apply to JO and PO).

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record JobOrderLineItemDto(
    int ItemNo,
    string? Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount);

public sealed record JobOrderDto(
    Guid Id,
    string JoNumber,
    DateOnly JoDate,
    Guid? PurchaseRequestId,
    string? JobRequestNo,
    string? RequisitioningOffice,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    ModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OursBursNumber,
    DateOnly? OursBursDate,
    JobOrderStatus Status,
    IReadOnlyList<JobOrderLineItemDto> LineItems,
    decimal TotalAmount,
    string TotalAmountInWords,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    // Funds Available — Accountant
    Guid? FundsAvailableCertifiedById = null,
    string? FundsAvailableCertifiedByName = null,
    DateTimeOffset? FundsAvailableCertifiedOnUtc = null,
    string? FundsAvailableCertifiedByDesignation = null,
    // Approved — Authorized Official who issued the JO
    Guid? IssuedById = null,
    string? IssuedByName = null,
    DateTimeOffset? IssuedOnUtc = null,
    string? IssuedByDesignation = null,
    // Inspection — C.O./F.O. Inspector (self-contained, no IAR)
    Guid? InspectedById = null,
    string? InspectedByName = null,
    string? InspectedByDesignation = null,
    DateTimeOffset? InspectedOnUtc = null,
    string? InspectionInvoiceNo = null,
    DateOnly? InspectionInvoiceDate = null,
    DateOnly? DateInspected = null,
    string? InspectionFindings = null,
    bool FoundInOrder = false,
    // Acceptance — PMSDS-GSD / F.O. Supply Officer (self-contained, no IAR)
    Guid? AcceptedById = null,
    string? AcceptedByName = null,
    string? AcceptedByDesignation = null,
    DateTimeOffset? AcceptedOnUtc = null,
    string? AcceptanceInvoiceNo = null,
    DateOnly? DateReceived = null,
    bool IsCompleteDelivery = true,
    string? PartialDeliveryNote = null,
    string? CancellationReason = null);

public sealed record JobOrderSummaryDto(
    Guid Id,
    string JoNumber,
    DateOnly JoDate,
    string? JobRequestNo,
    string SupplierName,
    ModeOfProcurement ModeOfProcurement,
    JobOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc,
    bool HasSignedCopy = false);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record JobOrderLineItemRequest(
    string? Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost);

/// <summary>
/// Creates a Job Order. Both origins are supported: request-driven (<paramref name="PurchaseRequestId"/> set,
/// particulars pre-filled from the source PR by the caller) and standalone (no PR — supplier and particulars
/// entered directly). <paramref name="JobRequestNo"/> is the free-text reference printed on the form.
/// </summary>
public sealed record CreateJobOrderCommand(
    Guid? PurchaseRequestId,
    string? JobRequestNo,
    string? RequisitioningOffice,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    ModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OursBursNumber,
    IReadOnlyList<JobOrderLineItemRequest> LineItems,
    bool AllowDuplicate = false) : ICommand<JobOrderDto>;

public sealed record UpdateJobOrderCommand(
    Guid Id,
    string? JobRequestNo,
    string? RequisitioningOffice,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    ModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OursBursNumber,
    IReadOnlyList<JobOrderLineItemRequest> LineItems) : ICommand<JobOrderDto>;

/// <summary>Submit a Draft JO for funds-available certification. Moves Draft → PendingFundsAvailable.</summary>
public sealed record SubmitJobOrderCommand(Guid Id) : ICommand<JobOrderDto>;

/// <summary>
/// Accountant signs the "Funds Available" portion of the JO and (optionally) captures the BUR/ORS reference
/// and Fund Cluster. Moves JO from PendingFundsAvailable to PendingApproval.
/// </summary>
public sealed record CertifyJobOrderFundsAvailableCommand(
    Guid Id,
    string CertifiedByName,
    string? OursBursNumber = null,
    DateOnly? OursBursDate = null,
    string? FundCluster = null) : ICommand<JobOrderDto>;

/// <summary>Authorized Official approves and issues the JO. Moves PendingApproval → Issued.</summary>
public sealed record IssueJobOrderCommand(Guid Id) : ICommand<JobOrderDto>;

/// <summary>
/// C.O./F.O. Inspector records the inspection result on an Issued JO. The signatory is resolved from the
/// authenticated identity, never the request body. Moves Issued → Inspected.
/// </summary>
public sealed record InspectJobOrderCommand(
    Guid Id,
    string? InvoiceNo = null,
    DateOnly? InvoiceDate = null,
    DateOnly? DateInspected = null,
    string? Findings = null,
    bool FoundInOrder = true) : ICommand<JobOrderDto>;

/// <summary>
/// Supply Officer records acceptance of the completed work on an Inspected JO. The signatory is resolved
/// from the authenticated identity, never the request body. Moves Inspected → Completed.
/// </summary>
public sealed record AcceptJobOrderCommand(
    Guid Id,
    string? InvoiceNo = null,
    DateOnly? DateReceived = null,
    bool IsCompleteDelivery = true,
    string? PartialDeliveryNote = null) : ICommand<JobOrderDto>;

public sealed record CancelJobOrderCommand(Guid Id, string? Reason = null) : ICommand<JobOrderDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetJobOrderQuery(Guid Id) : IQuery<JobOrderDto?>;

public sealed class SearchJobOrdersQuery : IQuery<PagedResponse<JobOrderSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public Guid? SupplierId { get; set; }
    public JobOrderStatus? Status { get; set; }
    public ModeOfProcurement? ModeOfProcurement { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
