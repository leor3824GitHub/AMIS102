using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.Expendable.Contracts.v1.Requests;

public record SupplyRequestItemDto(
    Guid ProductId,
    int RequestedQuantity,
    int ApprovedQuantity,
    int FulfilledQuantity,
    string? Notes);

public record SupplyRequestDto(
    Guid Id,
    string RequestNumber,
    string EmployeeId,
    string DepartmentId,
    DateTimeOffset RequestDate,
    DateTimeOffset? NeededByDate,
    string Status,
    string? BusinessJustification,
    string? RejectionReason,
    string? ApprovedBy,
    DateTimeOffset? ApprovedOnUtc,
    DateTimeOffset? FulfilledOnUtc,
    Guid? WarehouseLocationId,
    List<SupplyRequestItemDto> Items,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);

public record CreateSupplyRequestCommand(
    string DepartmentId,
    string? BusinessJustification = null,
    DateTimeOffset? NeededByDate = null) : ICommand<SupplyRequestDto>;

public record AddSupplyRequestItemCommand(
    Guid RequestId,
    Guid ProductId,
    int Quantity,
    string? Notes = null) : ICommand<Unit>;

public record RemoveSupplyRequestItemCommand(
    Guid RequestId,
    Guid ProductId) : ICommand<Unit>;

public record SubmitSupplyRequestCommand(Guid Id) : ICommand<Unit>;

public record ApproveSupplyRequestCommand(
    Guid Id,
    Dictionary<Guid, int> ApprovedQuantities,
    Guid WarehouseLocationId) : ICommand<Unit>;

public record RejectSupplyRequestCommand(
    Guid Id,
    string? Reason = null) : ICommand<Unit>;

public record MarkSupplyRequestFulfilledCommand(Guid Id) : ICommand<Unit>;

public record CancelSupplyRequestCommand(Guid Id) : ICommand<Unit>;

public record GetSupplyRequestQuery(Guid Id) : IQuery<SupplyRequestDto?>;

public sealed class SearchSupplyRequestsQuery : IPagedQuery, IQuery<PagedResponse<SupplyRequestDto>>
{
    public string? Status { get; set; }
    public string? EmployeeId { get; set; }
    public string? DepartmentId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetEmployeeSupplyRequestsQuery : IPagedQuery, IQuery<PagedResponse<SupplyRequestDto>>
{
    public string? EmployeeId { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

// ============= FULFILL =============

/// <summary>Supply officer fulfills an approved supply request — issues from warehouse and records per-employee receipt</summary>
public record FulfillSupplyRequestCommand(
    Guid SupplyRequestId,
    Guid? WarehouseLocationId = null,
    string? Notes = null
) : ICommand<FulfillSupplyRequestResponse>;

public record FulfillmentItemResultDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int QuantityIssued,
    decimal UnitPrice,
    decimal TotalValue
);

public record FulfillSupplyRequestResponse(
    Guid SupplyRequestId,
    string RequestNumber,
    string EmployeeId,
    string DepartmentId,
    DateTimeOffset FulfilledOnUtc,
    List<FulfillmentItemResultDto> Items,
    decimal TotalIssuedValue
);

// ============= DEPARTMENT ISSUANCE REPORT =============

/// <summary>Aggregated issuance report grouped by department — for supply officer reporting</summary>
public sealed class GetDepartmentIssuanceReportQuery : IPagedQuery, IQuery<PagedResponse<DepartmentIssuanceSummaryDto>>
{
    public string? DepartmentId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public record DepartmentIssuanceSummaryDto(
    string DepartmentId,
    int TotalRequestsFulfilled,
    int TotalItemsIssued,
    decimal TotalValue,
    List<DepartmentProductBreakdownDto> Products
);

public record DepartmentProductBreakdownDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int TotalQuantityIssued,
    decimal TotalValue,
    string Unit,
    decimal UnitCost
);

// ============= EMPLOYEE ISSUANCE HISTORY =============

/// <summary>Per-employee issuance history — what was issued, when, and from which request</summary>
public sealed class GetEmployeeIssuanceHistoryQuery : IPagedQuery, IQuery<PagedResponse<EmployeeIssuanceDto>>
{
    public string? EmployeeId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public record EmployeeIssuanceDto(
    Guid RequestId,
    string RequestNumber,
    string EmployeeId,
    string DepartmentId,
    DateTimeOffset FulfilledOnUtc,
    List<IssuanceItemDto> Items,
    decimal TotalValue
);

public record IssuanceItemDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int QuantityIssued,
    decimal UnitPrice,
    decimal TotalValue
);

// ============= PHYSICAL COUNT REPORT =============

/// <summary>Physical count report — all products with system balance vs on-hand count</summary>
public sealed class GetPhysicalCountReportQuery : IQuery<List<PhysicalCountItemDto>>
{
    public Guid? WarehouseLocationId { get; set; }
}

public record PhysicalCountItemDto(
    int ArticleNumber,
    string Description,
    string StockNo,
    string UnitOfMeasure,
    decimal UnitValue,
    int BalancePerCard,
    int OnHandPerCount,
    int ShortageQuantity,
    decimal ShortageValue,
    string? Remarks
);

