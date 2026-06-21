using AMIS.Framework.Core.Domain;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;

namespace AMIS.Modules.BudgetDisbursement.Domain.BudgetUtilizationRequests;

public sealed class BudgetUtilizationRequest : AggregateRoot<Guid>, IAuditableEntity
{
    public string BurNumber { get; private set; } = default!;
    public DateOnly BurDate { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public string PurchaseOrderNumber { get; private set; } = default!;
    public Guid? DisbursementVoucherId { get; private set; }
    public string? DisbursementVoucherNumber { get; private set; }
    public string AllotmentClass { get; private set; } = default!;
    public string UacsObjectCode { get; private set; } = default!;
    public string? ResponsibilityCenter { get; private set; }
    public string Particulars { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public BudgetUtilizationRequestStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private BudgetUtilizationRequest() { }

    public static BudgetUtilizationRequest Create(
        string burNumber,
        Guid purchaseOrderId,
        string purchaseOrderNumber,
        DateOnly burDate,
        string allotmentClass,
        string uacsObjectCode,
        string? responsibilityCenter,
        string particulars,
        decimal amount,
        string? remarks)
    {
        // A BUR is the first step of the disburse flow: it obligates budget against a PO. The linked
        // disbursement voucher is assigned later, when the DV is raised against this BUR (see Utilize).
        return new BudgetUtilizationRequest
        {
            Id = Guid.NewGuid(),
            BurNumber = burNumber,
            PurchaseOrderId = purchaseOrderId,
            PurchaseOrderNumber = purchaseOrderNumber,
            BurDate = burDate,
            AllotmentClass = allotmentClass,
            UacsObjectCode = uacsObjectCode,
            ResponsibilityCenter = responsibilityCenter,
            Particulars = particulars,
            Amount = amount,
            Remarks = remarks,
            Status = BudgetUtilizationRequestStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Obligate()
    {
        if (Status != BudgetUtilizationRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft BURs can be obligated.");

        Status = BudgetUtilizationRequestStatus.Obligated;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Utilize(Guid disbursementVoucherId, string disbursementVoucherNumber)
    {
        if (Status != BudgetUtilizationRequestStatus.Obligated)
            throw new InvalidOperationException("Only Obligated BURs can be utilized.");

        Status = BudgetUtilizationRequestStatus.Utilized;
        DisbursementVoucherId = disbursementVoucherId;
        DisbursementVoucherNumber = disbursementVoucherNumber;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Reverses a utilization: when the linked DV is cancelled, the obligation is released
    /// back to Obligated and the DV link cleared, so a new DV can be raised against this BUR.</summary>
    public void Release()
    {
        if (Status != BudgetUtilizationRequestStatus.Utilized)
            throw new InvalidOperationException("Only Utilized BURs can be released.");

        Status = BudgetUtilizationRequestStatus.Obligated;
        DisbursementVoucherId = null;
        DisbursementVoucherNumber = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string remarks)
    {
        if (Status == BudgetUtilizationRequestStatus.Utilized || Status == BudgetUtilizationRequestStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a BUR with status '{Status}'.");

        Status = BudgetUtilizationRequestStatus.Cancelled;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

