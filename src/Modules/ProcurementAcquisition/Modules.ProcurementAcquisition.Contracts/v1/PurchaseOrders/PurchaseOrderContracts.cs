using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum PurchaseOrderStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyDelivered = 2,
    Fulfilled = 3,
    Cancelled = 4
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
    decimal Amount);

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
    PurchaseOrderStatus Status,
    IReadOnlyList<PurchaseOrderLineItemDto> LineItems,
    decimal TotalAmount,
    string TotalAmountInWords,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

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
    decimal UnitCost);

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
    IReadOnlyList<PurchaseOrderLineItemRequest> LineItems) : ICommand<PurchaseOrderDto>;

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
