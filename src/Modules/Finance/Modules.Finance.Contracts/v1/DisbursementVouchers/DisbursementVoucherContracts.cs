using Mediator;

namespace AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;

// Enums
public enum DisbursementVoucherStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Paid = 3,
    Returned = 4,
    Cancelled = 5
}

// DTOs
public sealed record DisbursementVoucherDto(
    Guid Id,
    string DvNumber,
    DateOnly DvDate,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid BudgetUtilizationRecordId,
    string BurNumber,
    string FundCluster,
    string Payee,
    string? TinNo,
    string? PayeeAddress,
    string Particulars,
    decimal Amount,
    string ModeOfPayment,
    DisbursementVoucherStatus Status,
    string? Remarks,
    DateOnly? PaidDate,
    DateTime CreatedOn,
    DateTime? LastModifiedOn);

public sealed record DisbursementVoucherListItemDto(
    Guid Id,
    string DvNumber,
    DateOnly DvDate,
    string PurchaseOrderNumber,
    string Payee,
    decimal Amount,
    DisbursementVoucherStatus Status);

// Commands
// A DV is raised against an obligated Budget Utilization Record. The purchase order, payee and amount
// are derived server-side from that BUR — the client only identifies the BUR (BUR first, then DV).
public sealed record CreateDisbursementVoucherCommand(
    Guid BudgetUtilizationRecordId,
    string BurNumber,
    DateOnly DvDate,
    string FundCluster,
    string Payee,
    string? TinNo,
    string? PayeeAddress,
    string Particulars,
    decimal Amount,
    string ModeOfPayment,
    string? Remarks) : ICommand<Guid>;

public sealed record UpdateDisbursementVoucherCommand(
    Guid Id,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    DateOnly DvDate,
    string FundCluster,
    string Payee,
    string? TinNo,
    string? PayeeAddress,
    string Particulars,
    decimal Amount,
    string ModeOfPayment,
    string? Remarks) : ICommand;

public sealed record ApproveDisbursementVoucherCommand(Guid Id) : ICommand;

public sealed record PayDisbursementVoucherCommand(
    Guid Id,
    DateOnly PaidDate,
    string? Remarks) : ICommand;

public sealed record ReturnDisbursementVoucherCommand(
    Guid Id,
    string Remarks) : ICommand;

public sealed record CancelDisbursementVoucherCommand(
    Guid Id,
    string Remarks) : ICommand;

// Queries
public sealed record GetDisbursementVoucherByIdQuery(Guid Id) : IQuery<DisbursementVoucherDto>;

public sealed record SearchDisbursementVouchersQuery(
    string? Keyword,
    DisbursementVoucherStatus? Status,
    Guid? PurchaseOrderId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<DisbursementVoucherSearchResult>;

public sealed record DisbursementVoucherSearchResult(
    List<DisbursementVoucherListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

