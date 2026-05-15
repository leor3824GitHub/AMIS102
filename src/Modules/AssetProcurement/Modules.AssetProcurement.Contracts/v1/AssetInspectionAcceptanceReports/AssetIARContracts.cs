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
    Rejected = 2,             // legacy whole-IAR rejection — superseded by per-line Rejected; kept for back-compat
    PendingInspection = 3,
    Inspected = 4,
    Cancelled = 5
}

public enum LineInspectionResult
{
    Pending = 0,
    Passed = 1,
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
    string? StockPropertyNo,
    LineInspectionResult InspectionResult = LineInspectionResult.Pending,
    DateTimeOffset? InspectedOnUtc = null,
    Guid? InspectedById = null);

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
    DateTimeOffset? LastModifiedOnUtc,
    DateTimeOffset? SubmittedForInspectionOnUtc = null,
    DateTimeOffset? InspectedOnUtc = null,
    DateTimeOffset? AcceptedOnUtc = null,
    DateTimeOffset? CancelledOnUtc = null);

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
// Commands — existing
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
// Commands — stage workflow (new)
// ──────────────────────────────────────────────────────────────────────────────

public sealed record SubmitIARForInspectionCommand(Guid Id) : ICommand<AssetIARDto>;

public sealed record ReassignInspectorCommand(Guid Id, Guid NewInspectorId) : ICommand<AssetIARDto>;

public sealed record LineInspectionDecision(int ItemNo, LineInspectionResult Result, string? Remarks);

public sealed record RecordIARInspectionCommand(
    Guid Id,
    IReadOnlyList<LineInspectionDecision> Decisions) : ICommand<AssetIARDto>;

public sealed record AssignPropertyNoCommand(Guid Id, int ItemNo, string PropertyNo) : ICommand<AssetIARDto>;

public sealed record ExpandLineByQuantityCommand(Guid Id, int ItemNo) : ICommand<AssetIARDto>;

public sealed record CancelAssetIARCommand(Guid Id) : ICommand<AssetIARDto>;

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
