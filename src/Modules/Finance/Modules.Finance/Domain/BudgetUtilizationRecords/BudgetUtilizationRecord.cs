using AMIS.Framework.Core.Domain;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;

namespace AMIS.Modules.Finance.Domain.BudgetUtilizationRecords;

public sealed class BudgetUtilizationRecord : AggregateRoot<Guid>, IAuditableEntity
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
    public BudgetUtilizationRecordStatus Status { get; private set; }
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

    private BudgetUtilizationRecord() { }

    public static BudgetUtilizationRecord Create(
        string burNumber,
        Guid purchaseOrderId,
        string purchaseOrderNumber,
        Guid? disbursementVoucherId,
        string? disbursementVoucherNumber,
        DateOnly burDate,
        string allotmentClass,
        string uacsObjectCode,
        string? responsibilityCenter,
        string particulars,
        decimal amount,
        string? remarks)
    {
        return new BudgetUtilizationRecord
        {
            Id = Guid.NewGuid(),
            BurNumber = burNumber,
            PurchaseOrderId = purchaseOrderId,
            PurchaseOrderNumber = purchaseOrderNumber,
            DisbursementVoucherId = disbursementVoucherId,
            DisbursementVoucherNumber = disbursementVoucherNumber,
            BurDate = burDate,
            AllotmentClass = allotmentClass,
            UacsObjectCode = uacsObjectCode,
            ResponsibilityCenter = responsibilityCenter,
            Particulars = particulars,
            Amount = amount,
            Remarks = remarks,
            Status = BudgetUtilizationRecordStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Obligate()
    {
        if (Status != BudgetUtilizationRecordStatus.Draft)
            throw new InvalidOperationException("Only Draft BURs can be obligated.");

        Status = BudgetUtilizationRecordStatus.Obligated;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Utilize(Guid disbursementVoucherId, string disbursementVoucherNumber)
    {
        if (Status != BudgetUtilizationRecordStatus.Obligated)
            throw new InvalidOperationException("Only Obligated BURs can be utilized.");

        Status = BudgetUtilizationRecordStatus.Utilized;
        DisbursementVoucherId = disbursementVoucherId;
        DisbursementVoucherNumber = disbursementVoucherNumber;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string remarks)
    {
        if (Status == BudgetUtilizationRecordStatus.Utilized || Status == BudgetUtilizationRecordStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a BUR with status '{Status}'.");

        Status = BudgetUtilizationRecordStatus.Cancelled;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

