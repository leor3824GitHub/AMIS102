using FSH.Framework.Core.Domain;
using FSH.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace FSH.Modules.Finance.Domain.DisbursementVouchers;

public sealed class DisbursementVoucher : AggregateRoot<Guid>, IAuditableEntity
{
    public string DvNumber { get; private set; } = default!;
    public DateOnly DvDate { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public string PurchaseOrderNumber { get; private set; } = default!;
    public string FundCluster { get; private set; } = default!;
    public string Payee { get; private set; } = default!;
    public string? TinNo { get; private set; }
    public string? PayeeAddress { get; private set; }
    public string Particulars { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string ModeOfPayment { get; private set; } = default!;
    public DisbursementVoucherStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public DateOnly? PaidDate { get; private set; }
    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private DisbursementVoucher() { }

    public static DisbursementVoucher Create(
        string dvNumber,
        Guid purchaseOrderId,
        string purchaseOrderNumber,
        DateOnly dvDate,
        string fundCluster,
        string payee,
        string? tinNo,
        string? payeeAddress,
        string particulars,
        decimal amount,
        string modeOfPayment,
        string? remarks)
    {
        return new DisbursementVoucher
        {
            Id = Guid.NewGuid(),
            DvNumber = dvNumber,
            PurchaseOrderId = purchaseOrderId,
            PurchaseOrderNumber = purchaseOrderNumber,
            DvDate = dvDate,
            FundCluster = fundCluster,
            Payee = payee,
            TinNo = tinNo,
            PayeeAddress = payeeAddress,
            Particulars = particulars,
            Amount = amount,
            ModeOfPayment = modeOfPayment,
            Remarks = remarks,
            Status = DisbursementVoucherStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Approve()
    {
        if (Status != DisbursementVoucherStatus.Draft && Status != DisbursementVoucherStatus.ForApproval)
            throw new InvalidOperationException($"Cannot approve a DV with status '{Status}'.");

        Status = DisbursementVoucherStatus.Approved;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Pay(DateOnly paidDate, string? remarks)
    {
        if (Status != DisbursementVoucherStatus.Approved)
            throw new InvalidOperationException("Only Approved disbursement vouchers can be marked as paid.");

        Status = DisbursementVoucherStatus.Paid;
        PaidDate = paidDate;
        Remarks = remarks ?? Remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Return(string remarks)
    {
        if (Status == DisbursementVoucherStatus.Paid || Status == DisbursementVoucherStatus.Cancelled)
            throw new InvalidOperationException($"Cannot return a DV with status '{Status}'.");

        Status = DisbursementVoucherStatus.Returned;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string remarks)
    {
        if (Status == DisbursementVoucherStatus.Paid || Status == DisbursementVoucherStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a DV with status '{Status}'.");

        Status = DisbursementVoucherStatus.Cancelled;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
