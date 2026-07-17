using AMIS.Framework.Core.Domain;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;

namespace AMIS.Modules.BudgetDisbursement.Domain.DisbursementVouchers;

public sealed class DisbursementVoucher : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISignedCopyHolder
{
    public string TenantId { get; private set; } = default!;
    public string DvNumber { get; private set; } = default!;
    public DateOnly DvDate { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public string PurchaseOrderNumber { get; private set; } = default!;
    // The DV is raised against an obligated Budget Utilization Request (BUR). The PO above is inherited
    // from that BUR — the BUR is the entry point of the disburse flow (BUR first, then DV).
    public Guid BudgetUtilizationRequestId { get; private set; }
    public string BurNumber { get; private set; } = default!;
    public string FundCluster { get; private set; } = default!;
    public string Payee { get; private set; } = default!;
    public string? TinNo { get; private set; }
    public string? PayeeAddress { get; private set; }
    public string Particulars { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string ModeOfPayment { get; private set; } = default!;

    // Configurable deduction lines (tax, withholding, fees). Owned by the aggregate — replaced
    // wholesale on edit. The EF navigation binds to this backing field.
    private readonly List<DvDeduction> _deductions = [];
    public IReadOnlyCollection<DvDeduction> Deductions => _deductions.AsReadOnly();

    /// <summary>Sum of every deduction line resolved against the gross <see cref="Amount"/>.</summary>
    public decimal TotalDeductions => _deductions.Sum(d => d.ComputeAmount(Amount));

    /// <summary>Net payable after deductions — the "Amount Due" printed on the voucher.</summary>
    public decimal AmountDue => Amount - TotalDeductions;
    public DisbursementVoucherStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public DateOnly? PaidDate { get; private set; }
    public byte[] Version { get; private set; } = [];

    // IAuditableEntity
    /// <summary>The uploaded wet-signed copy of this document of record; null until one is uploaded.</summary>
    public SignedCopy? SignedCopy { get; private set; }

    /// <summary>Attaches or replaces the signed copy (one current copy per document).</summary>
    public void SetSignedCopy(SignedCopy copy) => SignedCopy = copy;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public bool IsDeleted { get; private set; }

    private DisbursementVoucher() { }

    public static DisbursementVoucher Create(
        string tenantId,
        string dvNumber,
        Guid purchaseOrderId,
        string purchaseOrderNumber,
        Guid budgetUtilizationRequestId,
        string burNumber,
        DateOnly dvDate,
        string fundCluster,
        string payee,
        string? tinNo,
        string? payeeAddress,
        string particulars,
        decimal amount,
        string modeOfPayment,
        string? remarks,
        IEnumerable<DvDeduction>? deductions = null)
    {
        var dv = new DisbursementVoucher
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DvNumber = dvNumber,
            PurchaseOrderId = purchaseOrderId,
            PurchaseOrderNumber = purchaseOrderNumber,
            BudgetUtilizationRequestId = budgetUtilizationRequestId,
            BurNumber = burNumber,
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
        dv.ReplaceDeductions(deductions);
        return dv;
    }

    /// <summary>Edits the voucher's contents. Only allowed while still in Draft — once submitted for
    /// approval the figures are locked. The DV number is immutable (assigned at creation).</summary>
    public void Update(
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
        string? remarks,
        IEnumerable<DvDeduction>? deductions = null)
    {
        if (Status != DisbursementVoucherStatus.Draft)
            throw new InvalidOperationException("Only Draft disbursement vouchers can be edited.");

        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderNumber = purchaseOrderNumber;
        DvDate = dvDate;
        FundCluster = fundCluster;
        Payee = payee;
        TinNo = tinNo;
        PayeeAddress = payeeAddress;
        Particulars = particulars;
        Amount = amount;
        ModeOfPayment = modeOfPayment;
        Remarks = remarks;
        ReplaceDeductions(deductions);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Replaces the voucher's deduction lines wholesale. EF deletes the removed rows and
    /// inserts the new ones when the aggregate is saved.</summary>
    public void ReplaceDeductions(IEnumerable<DvDeduction>? deductions)
    {
        _deductions.Clear();
        if (deductions is not null)
            _deductions.AddRange(deductions);
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
        if (Status == DisbursementVoucherStatus.Draft || Status == DisbursementVoucherStatus.Paid || Status == DisbursementVoucherStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a DV with status '{Status}'.");

        Status = DisbursementVoucherStatus.Cancelled;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}

