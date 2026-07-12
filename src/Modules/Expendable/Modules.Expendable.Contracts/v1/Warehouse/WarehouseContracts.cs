using AMIS.Framework.Core.Abstractions;
using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.Expendable.Contracts.v1.Warehouse;

// ============= COMMANDS (Write Operations) =============

// Receiving + inspection of purchased stock now lives in ProcurementAcquisition (IAR). Accepted Supply IAR
// lines flow into ProductInventory via SupplyIARAcceptedEvent. The commands below are issue-side only.

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

/// <summary>Unpaged list of warehouse locations that hold inventory — for bounded warehouse pickers.</summary>
public sealed record ListWarehouseLocationsQuery : IQuery<IReadOnlyList<WarehouseLocationDto>>;

/// <summary>Inventory rows (all warehouses) for a bounded set of products — replaces "fetch every row and filter client-side".</summary>
public sealed record GetProductInventoriesByProductsQuery(
    IReadOnlyCollection<Guid> ProductIds
) : IQuery<IReadOnlyList<ProductInventoryDto>>;

// ============= DTOs =============

public record WarehouseLocationDto(Guid Id, string Name);

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


