using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum AssetIARStatus
{
    Draft = 0,
    Accepted = 1,
    Rejected = 2
}

// ──────────────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetIARLineItemDto(
    int ItemNo,
    string Description,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? SerialNo,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    decimal Amount,
    string? InspectionRemarks,
    string? StockPropertyNo);

public sealed record AssetIARDto(
    Guid Id,
    string IarNumber,
    DateOnly IarDate,
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    Guid InspectedById,
    string InspectedByName,
    Guid ReceivedById,
    string ReceivedByName,
    string? DeliveryReceiptNo,
    DateOnly? DeliveryDate,
    AssetIARStatus Status,
    string? Remarks,
    IReadOnlyList<AssetIARLineItemDto> LineItems,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record AssetIARSummaryDto(
    Guid Id,
    string IarNumber,
    DateOnly IarDate,
    string PoNumber,
    string SupplierName,
    int LineItemCount,
    decimal TotalAmount,
    AssetIARStatus Status,
    DateTimeOffset CreatedOnUtc);

// ──────────────────────────────────────────────────────────────────────────────
// Commands
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetIARLineItemRequest(
    string Description,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? SerialNo,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    string? InspectionRemarks,
    string? StockPropertyNo = null);

public sealed record CreateAssetIARCommand(
    Guid PurchaseOrderId,
    Guid InspectedById,
    Guid ReceivedById,
    string? DeliveryReceiptNo,
    DateOnly? DeliveryDate,
    string? Remarks,
    IReadOnlyList<AssetIARLineItemRequest> LineItems) : ICommand<AssetIARDto>;

public sealed record UpdateAssetIARCommand(
    Guid Id,
    Guid InspectedById,
    Guid ReceivedById,
    string? DeliveryReceiptNo,
    DateOnly? DeliveryDate,
    string? Remarks,
    IReadOnlyList<AssetIARLineItemRequest> LineItems) : ICommand<AssetIARDto>;

public sealed record AcceptAssetIARCommand(Guid Id) : ICommand<AssetIARDto>;

public sealed record RejectAssetIARCommand(Guid Id, string Reason) : ICommand<AssetIARDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetAssetIARQuery(Guid Id) : IQuery<AssetIARDto?>;

public sealed class SearchAssetIARsQuery : IQuery<PagedResponse<AssetIARSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public AssetIARStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ──────────────────────────────────────────────────────────────────────────────
// Integration Event
// ──────────────────────────────────────────────────────────────────────────────

public sealed record AssetIARAcceptedEventItem(
    string Description,
    string? TechnicalSpecifications,
    string? Brand,
    string? Model,
    string? SerialNo,
    string? PropertyClassHint,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    string? StockPropertyNo = null);

public sealed record AssetIARAcceptedEvent(
    Guid IARId,
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    IReadOnlyList<AssetIARAcceptedEventItem> AcceptedItems,
    string? TenantId,
    string CorrelationId = "") : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public string Source { get; } = "AssetProcurement";
}

