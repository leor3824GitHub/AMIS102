using Mediator;

namespace AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;

// Enums
public enum BudgetUtilizationRecordStatus
{
    Draft = 0,
    Obligated = 1,
    Utilized = 2,
    Cancelled = 3
}

// DTOs
public sealed record BudgetUtilizationRecordDto(
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
    BudgetUtilizationRecordStatus Status,
    string? Remarks,
    DateTime CreatedOn,
    DateTime? LastModifiedOn);

public sealed record BudgetUtilizationRecordListItemDto(
    Guid Id,
    string BurNumber,
    DateOnly BurDate,
    string PurchaseOrderNumber,
    string AllotmentClass,
    decimal Amount,
    BudgetUtilizationRecordStatus Status);

// Commands
public sealed record CreateBudgetUtilizationRecordCommand(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid? DisbursementVoucherId,
    string? DisbursementVoucherNumber,
    DateOnly BurDate,
    string AllotmentClass,
    string UacsObjectCode,
    string? ResponsibilityCenter,
    string Particulars,
    decimal Amount,
    string? Remarks) : ICommand<Guid>;

public sealed record ObligateBudgetUtilizationRecordCommand(Guid Id) : ICommand;

public sealed record UtilizeBudgetUtilizationRecordCommand(
    Guid Id,
    Guid DisbursementVoucherId,
    string DisbursementVoucherNumber) : ICommand;

public sealed record CancelBudgetUtilizationRecordCommand(
    Guid Id,
    string Remarks) : ICommand;

// Queries
public sealed record GetBudgetUtilizationRecordByIdQuery(Guid Id) : IQuery<BudgetUtilizationRecordDto>;

public sealed record SearchBudgetUtilizationRecordsQuery(
    string? Keyword,
    BudgetUtilizationRecordStatus? Status,
    Guid? PurchaseOrderId,
    string? AllotmentClass,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<BudgetUtilizationRecordSearchResult>;

public sealed record BudgetUtilizationRecordSearchResult(
    List<BudgetUtilizationRecordListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

