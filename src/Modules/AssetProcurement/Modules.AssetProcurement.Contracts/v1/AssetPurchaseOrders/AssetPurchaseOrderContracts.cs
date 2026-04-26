using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum AssetPurchaseOrderStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyDelivered = 2,
    Fulfilled = 3,
    Cancelled = 4
}

public enum AssetModeOfProcurement
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

public sealed record AssetPurchaseOrderLineItemDto(
    int ItemNo,
    string Unit,
    string Description,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? PropertyClassHint,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount);

public sealed record AssetPurchaseOrderDto(
    Guid Id,
    string PoNumber,
    DateOnly PoDate,
    Guid PurchaseRequestId,
    string PrNumber,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    AssetModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OblRequestNumber,
    AssetPurchaseOrderStatus Status,
    IReadOnlyList<AssetPurchaseOrderLineItemDto> LineItems,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record AssetPurchaseOrderSummaryDto(
    Guid Id,
    string PoNumber,
    DateOnly PoDate,
    string PrNumber,
    string SupplierName,
    AssetModeOfProcurement ModeOfProcurement,
    AssetPurchaseOrderStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetPurchaseOrderLineItemRequest(
    string Unit,
    string Description,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? PropertyClassHint,
    decimal Quantity,
    decimal UnitCost);

public sealed record CreateAssetPurchaseOrderCommand(
    Guid PurchaseRequestId,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    AssetModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OblRequestNumber,
    IReadOnlyList<AssetPurchaseOrderLineItemRequest> LineItems) : ICommand<AssetPurchaseOrderDto>;

public sealed record UpdateAssetPurchaseOrderCommand(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string SupplierAddress,
    string? SupplierTin,
    AssetModeOfProcurement ModeOfProcurement,
    string PlaceOfDelivery,
    DateOnly? DateOfDelivery,
    string DeliveryTerm,
    string PaymentTerm,
    string? FundCluster,
    string? OblRequestNumber,
    IReadOnlyList<AssetPurchaseOrderLineItemRequest> LineItems) : ICommand<AssetPurchaseOrderDto>;

public sealed record IssueAssetPurchaseOrderCommand(Guid Id) : ICommand<AssetPurchaseOrderDto>;

public sealed record CancelAssetPurchaseOrderCommand(Guid Id, string? Reason = null) : ICommand<AssetPurchaseOrderDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetAssetPurchaseOrderQuery(Guid Id) : IQuery<AssetPurchaseOrderDto?>;

public sealed class SearchAssetPurchaseOrdersQuery : IQuery<PagedResponse<AssetPurchaseOrderSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseRequestId { get; set; }
    public Guid? SupplierId { get; set; }
    public AssetPurchaseOrderStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
