using FSH.Framework.Core.Abstractions;
using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Expendable.Contracts.v1.Warehouse;

// ============= COMMANDS (Write Operations) =============

/// <summary>Record inspection results for a purchase line item</summary>
public sealed record RecordInspectionCommand(
    Guid PurchaseId,
    Guid ProductId,
    int QuantityAccepted,
    int QuantityRejected,
    string RejectionReason,
    string? Notes = null,
    List<InspectionDefectDto>? Defects = null
) : ICommand<RecordInspectionResponse>;

public record InspectionDefectDto(int UnitNumber, string Description, string? Severity = null);

public record RecordInspectionResponse(
    Guid InspectionId,
    string Status,
    int QuantityAccepted,
    int QuantityRejected
);

/// <summary>Reserve product inventory for supply request allocation</summary>
public sealed record ReserveProductInventoryCommand(
    Guid ProductInventoryId,
    int QuantityToReserve
) : ICommand<ReserveProductInventoryResponse>;

public record ReserveProductInventoryResponse(
    Guid ProductInventoryId,
    int QuantityAvailable,
    int QuantityReserved
);

/// <summary>Cancel reservation if supply request is rejected</summary>
public sealed record CancelProductInventoryReservationCommand(
    Guid ProductInventoryId,
    int QuantityToRelease
) : ICommand<CancelProductInventoryReservationResponse>;

public record CancelProductInventoryReservationResponse(
    Guid ProductInventoryId,
    int QuantityAvailable,
    int QuantityReserved
);

/// <summary>Issue reserved inventory to employee</summary>
public sealed record IssueFromProductInventoryCommand(
    Guid ProductInventoryId,
    int QuantityToIssue
) : ICommand<IssueFromProductInventoryResponse>;

public record IssueFromProductInventoryResponse(
    Guid ProductInventoryId,
    int QuantityIssued,
    decimal AverageUnitPrice,
    decimal TotalIssuedValue
);

/// <summary>Mark rejected inventory as returned to supplier</summary>
public sealed record MarkRejectedInventoryReturnedCommand(
    Guid RejectedInventoryId,
    int? QuantityReturned = null,
    string? Notes = null
) : ICommand<MarkRejectedInventoryReturnedResponse>;

public record MarkRejectedInventoryReturnedResponse(
    Guid RejectedInventoryId,
    string Status
);

/// <summary>Mark rejected inventory as disposed</summary>
public sealed record MarkRejectedInventoryDisposedCommand(
    Guid RejectedInventoryId,
    string DisposalMethod,
    string? Notes = null
) : ICommand<MarkRejectedInventoryDisposedResponse>;

public record MarkRejectedInventoryDisposedResponse(
    Guid RejectedInventoryId,
    string Status
);

// ============= QUERIES (Read Operations) =============

/// <summary>Get product inventory by product and warehouse</summary>
public sealed record GetProductInventoryQuery(
    Guid ProductId,
    Guid WarehouseLocationId
) : IQuery<ProductInventoryDto?>;

/// <summary>Search product inventory with filters</summary>
public sealed class SearchProductInventoryQuery : IPagedQuery, IQuery<PagedResponse<ProductInventoryDto>>
{
    public Guid? WarehouseLocationId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

/// <summary>Get warehouse stock levels summary</summary>
public sealed class GetWarehouseStockLevelsQuery : IPagedQuery, IQuery<PagedResponse<ProductInventoryDto>>
{
    public Guid WarehouseLocationId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

/// <summary>Get rejected inventory awaiting disposition</summary>
public sealed class GetRejectedInventoryQuery : IPagedQuery, IQuery<PagedResponse<RejectedInventoryDto>>
{
    public Guid? WarehouseLocationId { get; set; }
    public string? Status { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

/// <summary>Get inspections pending processing</summary>
public sealed class GetPendingInspectionsQuery : IPagedQuery, IQuery<PagedResponse<PurchaseInspectionDto>>
{
    public Guid? WarehouseLocationId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

// ============= DTOs =============

public record ProductInventoryDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid WarehouseLocationId,
    string WarehouseLocationName,
    int QuantityAvailable,
    int QuantityReserved,
    int QuantityOnHand,
    int QuantityIssued,
    decimal TotalValue,
    decimal ReservedValue,
    decimal AverageUnitPrice,
    string Status,
    DateTimeOffset? FirstReceiptDate,
    DateTimeOffset? LastReceiptDate,
    DateTimeOffset? LastIssueDate
);

public record RejectedInventoryDto(
    Guid Id,
    Guid PurchaseId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid WarehouseLocationId,
    string WarehouseLocationName,
    int QuantityRejected,
    decimal UnitPrice,
    decimal TotalValue,
    string RejectionReason,
    string? Notes,
    string Status,
    DateTimeOffset RejectionDate,
    DateTimeOffset? DispositionDate,
    string? DispositionNotes
);

public record PurchaseInspectionDto(
    Guid Id,
    Guid PurchaseId,
    Guid ProductId,
    int QuantityReceivedForInspection,
    int QuantityAccepted,
    int QuantityRejected,
    string Status,
    string RejectionReason,
    string? Notes,
    DateTimeOffset InspectionDate,
    List<InspectionDefectDto> Defects
);

// ============= STOCK CARD REPORT =============

/// <summary>Complete stock card ledger for a single product — receipts + issuances with running balance</summary>
public sealed record GetStockCardQuery(Guid ProductId) : IQuery<StockCardDto?>;

public record StockCardDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    List<StockCardLineDto> Lines
);

public record StockCardLineDto(
    DateTimeOffset Date,
    string Reference,
    string TransactionType,     // "Receipt" or "Issue"
    string? Office,             // Department/employee for issuances
    // Beginning Balance (Receipt column)
    int ReceiptQty,
    decimal ReceiptUnitCost,
    decimal ReceiptTotalCost,
    // Issuance column
    int IssueQty,
    decimal IssueUnitCost,
    decimal IssueTotalCost,
    // Ending Balance (running balance)
    int BalanceQty,
    decimal BalanceUnitCost,
    decimal BalanceTotalCost
);

