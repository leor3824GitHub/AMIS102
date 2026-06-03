using AMIS.Framework.Eventing.Abstractions;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

// ──────────────────────────────────────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────────────────────────────────────

public enum InspectionAcceptanceReportStatus
{
    Draft = 0,
    Accepted = 1,
    // 2 = reserved (was used during early development, kept to avoid migration gaps)
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

public sealed record InspectionAcceptanceReportLineItemDto(
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
    Guid? InspectedById = null,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null,
    string? StockNumber = null);

public sealed record InspectionAcceptanceReportDto(
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
    InspectionAcceptanceReportStatus Status,
    string? Remarks,
    IReadOnlyList<InspectionAcceptanceReportLineItemDto> LineItems,
    decimal TotalAmount,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    DateTimeOffset? SubmittedForInspectionOnUtc = null,
    DateTimeOffset? InspectedOnUtc = null,
    DateTimeOffset? AcceptedOnUtc = null,
    DateTimeOffset? CancelledOnUtc = null,
    bool HasSignedCopy = false,
    ProcurementCategory Category = ProcurementCategory.Asset,
    // Acceptance signatory snapshot — the authenticated user who clicked Accept, frozen at the action.
    Guid? AcceptedById = null,
    string? AcceptedByName = null,
    string? AcceptedByDesignation = null);

public sealed record InspectionAcceptanceReportSummaryDto(
    Guid Id,
    string IarNumber,
    DateOnly IarDate,
    string PoNumber,
    string SupplierName,
    int LineItemCount,
    decimal TotalAmount,
    InspectionAcceptanceReportStatus Status,
    DateTimeOffset CreatedOnUtc,
    Guid AssignedInspectorId,
    bool HasSignedCopy = false,
    ProcurementCategory Category = ProcurementCategory.Asset);

// ──────────────────────────────────────────────────────────────────────────────
// Commands — existing
// ──────────────────────────────────────────────────────────────────────────────

public sealed record InspectionAcceptanceReportLineItemRequest(
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
    string? StockPropertyNo = null,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null,
    string? StockNumber = null);

public sealed record CreateInspectionAcceptanceReportCommand(
    Guid PurchaseOrderId,
    Guid InspectedById,
    Guid ReceivedById,
    string? DeliveryReceiptNo,
    DateOnly? DeliveryDate,
    string? Remarks,
    IReadOnlyList<InspectionAcceptanceReportLineItemRequest> LineItems) : ICommand<InspectionAcceptanceReportDto>;

public sealed record UpdateInspectionAcceptanceReportCommand(
    Guid Id,
    Guid InspectedById,
    Guid ReceivedById,
    string? DeliveryReceiptNo,
    DateOnly? DeliveryDate,
    string? Remarks,
    IReadOnlyList<InspectionAcceptanceReportLineItemRequest> LineItems) : ICommand<InspectionAcceptanceReportDto>;

public sealed record AcceptInspectionAcceptanceReportCommand(Guid Id) : ICommand<InspectionAcceptanceReportDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Commands — stage workflow (new)
// ──────────────────────────────────────────────────────────────────────────────

public sealed record SubmitIARForInspectionCommand(Guid Id) : ICommand<InspectionAcceptanceReportDto>;

public sealed record ReassignInspectorCommand(Guid Id, Guid NewInspectorId) : ICommand<InspectionAcceptanceReportDto>;

public sealed record LineInspectionDecision(int ItemNo, LineInspectionResult Result, string? Remarks);

public sealed record RecordIARInspectionCommand(
    Guid Id,
    IReadOnlyList<LineInspectionDecision> Decisions) : ICommand<InspectionAcceptanceReportDto>;

public sealed record AssignPropertyNoCommand(Guid Id, int ItemNo, string PropertyNo) : ICommand<InspectionAcceptanceReportDto>;

public sealed record ExpandLineByQuantityCommand(Guid Id, int ItemNo) : ICommand<InspectionAcceptanceReportDto>;

public sealed record CancelInspectionAcceptanceReportCommand(Guid Id) : ICommand<InspectionAcceptanceReportDto>;

// ──────────────────────────────────────────────────────────────────────────────
// Queries
// ──────────────────────────────────────────────────────────────────────────────

public sealed record GetInspectionAcceptanceReportQuery(Guid Id) : IQuery<InspectionAcceptanceReportDto?>;

public sealed class SearchInspectionAcceptanceReportsQuery : IQuery<PagedResponse<InspectionAcceptanceReportSummaryDto>>
{
    public string? Keyword { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public InspectionAcceptanceReportStatus? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// A single line item from an Accepted IAR, exposed for downstream modules (notably
/// AssetRegister Receiving Reports) to pre-populate forms without re-typing supplier
/// data. <see cref="ItemNo"/> uniquely identifies the line within its parent IAR.
/// </summary>
public sealed record AcceptedIARLineItemDto(
    Guid IARId,
    string IARNumber,
    DateOnly IARDate,
    int ItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    string? PropertyClassHint,
    string? SerialNo,
    string? Brand,
    string? Model,
    string? StockPropertyNo,
    string SupplierName,
    string? SupplierAddress,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null);

public sealed class SearchAcceptedIARLineItemsQuery : IQuery<PagedResponse<AcceptedIARLineItemDto>>
{
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
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
    string? StockPropertyNo = null,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null);

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
    public string Source { get; } = "Procurement";
}

// ──────────────────────────────────────────────────────────────────────────────
// Supply (expendable) acceptance — consumed by the Expendable module to land stock
// into ProductInventory. Mirrors AssetIARAcceptedEvent but carries the product StockNo
// (for matching) and no asset-only fields (property/serial). One line per accepted,
// non-rejected supply IAR line; quantities are NOT split per unit.
// ──────────────────────────────────────────────────────────────────────────────

public sealed record SupplyIARAcceptedEventItem(
    string? StockNumber,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitCost);

public sealed record SupplyIARAcceptedEvent(
    Guid IARId,
    string IarNumber,
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    IReadOnlyList<SupplyIARAcceptedEventItem> AcceptedItems,
    string? TenantId,
    string CorrelationId = "") : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public string Source { get; } = "Procurement";
}
