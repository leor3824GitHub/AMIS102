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

// How a deduction line's amount is derived from the voucher's gross amount.
public enum DvDeductionType
{
    // Value is a percentage (e.g. 5 = 5%) applied to the gross Amount.
    Percentage = 0,
    // Value is a flat peso figure deducted as-is.
    FixedAmount = 1
}

// A configurable deduction line on a voucher (e.g. "5% Withholding Tax", "1% Tax").
// Value is interpreted per Type: a percentage of the gross Amount, or a fixed peso figure.
public sealed record DvDeductionInput(
    string Name,
    DvDeductionType Type,
    decimal Value);

// A deduction line as returned to the client, with its computed peso Amount resolved server-side.
public sealed record DvDeductionDto(
    string Name,
    DvDeductionType Type,
    decimal Value,
    decimal Amount);

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
    DateTime? LastModifiedOn,
    IReadOnlyList<DvDeductionDto> Deductions,
    decimal TotalDeductions,
    decimal AmountDue);

public sealed record DisbursementVoucherListItemDto(
    Guid Id,
    string DvNumber,
    DateOnly DvDate,
    string PurchaseOrderNumber,
    string Payee,
    decimal Amount,
    DisbursementVoucherStatus Status,
    bool HasSignedCopy = false);

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
    string? Remarks,
    IReadOnlyList<DvDeductionInput>? Deductions = null) : ICommand<Guid>;

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
    string? Remarks,
    IReadOnlyList<DvDeductionInput>? Deductions = null) : ICommand;

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

// One status tally backing the status-filter chip badges.
public sealed record DisbursementVoucherStatusCountDto(DisbursementVoucherStatus Status, int Count);

// Returns DV counts grouped by status for the filter-chip badges. Keyword-scoped: applies the same
// keyword filter as the search so each badge matches what selecting that status would actually return.
public sealed record GetDisbursementVoucherStatusCountsQuery(string? Keyword = null)
    : IQuery<IReadOnlyList<DisbursementVoucherStatusCountDto>>;

