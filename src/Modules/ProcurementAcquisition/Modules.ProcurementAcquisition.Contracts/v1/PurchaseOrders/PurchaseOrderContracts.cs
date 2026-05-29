using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum PurchaseOrderStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyDelivered = 2,
    Fulfilled = 3,
    Cancelled = 4,
    PendingFundsAvailable = 5,  // submitted by buyer; awaiting Accountant funds-available certification
    PendingApproval = 6          // certified by Accountant; awaiting Issue (HoPE/Buyer)
}

public enum ModeOfProcurement
{
    DirectAcquisition = 0,
    SmallValueProcurement = 1,
    PublicBidding = 2,
    NegotiatedProcurement = 3,
    ShoppingA = 4,
    ShoppingB = 5
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record PurchaseOrderLineItemDto(
    int ItemNo,
    string? StockNumber,
    string Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount,
    Guid? CatalogItemId = null);

public sealed record PurchaseOrderDto(
    Guid Id,
    string PoNumber,
    DateOnly PoDate,
    Guid PurchaseRequestId,
    string PrNumber,
    Guid? CanvassRequestId,
    string? RivNumber,
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
    PurchaseOrderStatus Status,
    IReadOnlyList<PurchaseOrderLineItemDto> LineItems,
    decimal TotalAmount,
    string TotalAmountInWords,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    Guid? FundsAvailableCertifiedById = null,
    string? FundsAvailableCertifiedByName = null,
    DateTimeOffset? FundsAvailableCertifiedOnUtc = null,
    Guid? IssuedById = null,
    string? IssuedByName = null,
    DateTimeOffset? IssuedOnUtc = null,
    // Signatory designation snapshots — frozen at the action (faithful-reprint plan).
    string? FundsAvailableCertifiedByDesignation = null,
    string? IssuedByDesignation = null);

public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    string PoNumber,
    DateOnly PoDate,
    string PrNumber,
    string SupplierName,
    ModeOfProcurement ModeOfProcurement,
    PurchaseOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record PurchaseOrderLineItemRequest(
    string? StockNumber,
    string Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost,
    Guid? CatalogItemId = null);

public sealed record CreatePurchaseOrderCommand(
    Guid PurchaseRequestId,
    Guid? CanvassRequestId,
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
    IReadOnlyList<PurchaseOrderLineItemRequest> LineItems,
    bool AllowDuplicate = false) : ICommand<PurchaseOrderDto>;

public sealed record UpdatePurchaseOrderCommand(
    Guid Id,
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
    IReadOnlyList<PurchaseOrderLineItemRequest> LineItems) : ICommand<PurchaseOrderDto>;

/// <summary>Submit a Draft PO for funds-available certification. Moves Draft → PendingFundsAvailable.</summary>
public sealed record SubmitPurchaseOrderCommand(Guid Id) : ICommand<PurchaseOrderDto>;

/// <summary>
/// Accountant signs the "Funds Available" portion of the PO and (optionally) captures the ORS/BURS
/// reference and Fund Cluster. Moves PO from PendingFundsAvailable to PendingApproval. UACS object codes
/// are certified on the source PR (and held on the catalog), not on the supplier-facing PO.
/// </summary>
public sealed record CertifyPurchaseOrderFundsAvailableCommand(
    Guid Id,
    string CertifiedByName,
    string? OursBursNumber = null,
    DateOnly? OursBursDate = null,
    string? FundCluster = null) : ICommand<PurchaseOrderDto>;

public sealed record IssuePurchaseOrderCommand(Guid Id) : ICommand<PurchaseOrderDto>;

public sealed record CancelPurchaseOrderCommand(Guid Id, string? Reason = null) : ICommand<PurchaseOrderDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetPurchaseOrderQuery(Guid Id) : IQuery<PurchaseOrderDto?>;

public sealed class SearchPurchaseOrdersQuery : IQuery<PagedResponse<PurchaseOrderSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public Guid? SupplierId { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
    public ModeOfProcurement? ModeOfProcurement { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
