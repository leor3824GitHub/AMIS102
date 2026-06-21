using Mediator;

namespace AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;

// Enums
public enum BudgetUtilizationRequestStatus
{
    Draft = 0,
    Obligated = 1,
    Utilized = 2,
    Cancelled = 3
}

// DTOs
public sealed record BudgetUtilizationRequestDto(
    Guid Id,
    string BurNumber,
    DateOnly BurDate,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid? DisbursementVoucherId,
    string? DisbursementVoucherNumber,
    string AllotmentClass,
    string UacsObjectCode,
    string? ResponsibilityCenter,
    string Particulars,
    decimal Amount,
    BudgetUtilizationRequestStatus Status,
    string? Remarks,
    DateTime CreatedOn,
    DateTime? LastModifiedOn);

public sealed record BudgetUtilizationRequestListItemDto(
    Guid Id,
    string BurNumber,
    DateOnly BurDate,
    string PurchaseOrderNumber,
    string AllotmentClass,
    decimal Amount,
    BudgetUtilizationRequestStatus Status,
    bool HasSignedCopy = false);

// Commands
// A BUR is created up front against a purchase order to obligate budget. The disbursement voucher is
// linked later, when a DV is raised against this BUR — so no DV reference is supplied at creation.
public sealed record CreateBudgetUtilizationRequestCommand(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    DateOnly BurDate,
    string AllotmentClass,
    string UacsObjectCode,
    string? ResponsibilityCenter,
    string Particulars,
    decimal Amount,
    string? Remarks) : ICommand<Guid>;

public sealed record DeleteBudgetUtilizationRequestCommand(Guid Id) : ICommand;

public sealed record ObligateBudgetUtilizationRequestCommand(Guid Id) : ICommand;

public sealed record UtilizeBudgetUtilizationRequestCommand(
    Guid Id,
    Guid DisbursementVoucherId,
    string DisbursementVoucherNumber) : ICommand;

public sealed record CancelBudgetUtilizationRequestCommand(
    Guid Id,
    string Remarks) : ICommand;

// Queries
public sealed record GetBudgetUtilizationRequestByIdQuery(Guid Id) : IQuery<BudgetUtilizationRequestDto>;

public sealed record SearchBudgetUtilizationRequestsQuery(
    string? Keyword,
    BudgetUtilizationRequestStatus? Status,
    Guid? PurchaseOrderId,
    string? AllotmentClass,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<BudgetUtilizationRequestSearchResult>;

public sealed record BudgetUtilizationRequestSearchResult(
    List<BudgetUtilizationRequestListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

// One status tally backing the status-filter chip badges.
public sealed record BudgetUtilizationRequestStatusCountDto(BudgetUtilizationRequestStatus Status, int Count);

// Returns BUR counts grouped by status for the filter-chip badges. Keyword-scoped: applies the same
// keyword filter as the search so each badge matches what selecting that status would actually return.
public sealed record GetBudgetUtilizationRequestStatusCountsQuery(string? Keyword = null)
    : IQuery<IReadOnlyList<BudgetUtilizationRequestStatusCountDto>>;

