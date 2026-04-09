using FSH.Framework.Core.Domain;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace FSH.Modules.ProcurementAcquisition.Domain.Canvass;

public sealed class CanvassQuotationLineItem
{
    public int ItemNo { get; private set; }
    public string Description { get; private set; } = default!;
    public string Unit { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;

    private CanvassQuotationLineItem() { }

    public static CanvassQuotationLineItem Create(int itemNo, string description, string unit, decimal quantity, decimal unitPrice)
    {
        return new CanvassQuotationLineItem
        {
            ItemNo = itemNo,
            Description = description,
            Unit = unit,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

public sealed class CanvassQuotation : AggregateRoot<Guid>, IAuditableEntity
{
    public Guid CanvassRequestId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public string SupplierAddress { get; private set; } = default!;
    public string? TinNumber { get; private set; }
    public DateOnly QuotationDate { get; private set; }
    public string? DeliveryTerms { get; private set; }
    public bool IsAwarded { get; private set; }

    private readonly List<CanvassQuotationLineItem> _lineItems = [];
    public IReadOnlyList<CanvassQuotationLineItem> LineItems => _lineItems.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private CanvassQuotation() { }

    public static CanvassQuotation Create(
        Guid canvassRequestId,
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? tinNumber,
        DateOnly quotationDate,
        string? deliveryTerms,
        IEnumerable<(string Description, string Unit, decimal Quantity, decimal UnitPrice)> lineItems)
    {
        var quotation = new CanvassQuotation
        {
            Id = Guid.NewGuid(),
            CanvassRequestId = canvassRequestId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            SupplierAddress = supplierAddress,
            TinNumber = tinNumber,
            QuotationDate = quotationDate,
            DeliveryTerms = deliveryTerms,
            IsAwarded = false,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var (desc, unit, qty, price) in lineItems)
        {
            quotation._lineItems.Add(CanvassQuotationLineItem.Create(itemNo++, desc, unit, qty, price));
        }

        return quotation;
    }

    public void Update(
        string supplierName,
        string supplierAddress,
        string? tinNumber,
        DateOnly quotationDate,
        string? deliveryTerms,
        IEnumerable<(string Description, string Unit, decimal Quantity, decimal UnitPrice)> lineItems)
    {
        SupplierName = supplierName;
        SupplierAddress = supplierAddress;
        TinNumber = tinNumber;
        QuotationDate = quotationDate;
        DeliveryTerms = deliveryTerms;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var (desc, unit, qty, price) in lineItems)
        {
            _lineItems.Add(CanvassQuotationLineItem.Create(itemNo++, desc, unit, qty, price));
        }
    }

    public void MarkAwarded() => IsAwarded = true;

    public void ClearAwarded() => IsAwarded = false;
}

public sealed class CanvassRequest : AggregateRoot<Guid>, IAuditableEntity
{
    public string RivNumber { get; private set; } = default!;
    public Guid PurchaseRequestId { get; private set; }
    public DateOnly ReturnDeadline { get; private set; }
    public CanvassRequestStatus Status { get; private set; }
    public Guid? AwardedSupplierId { get; private set; }
    public byte[] Version { get; set; } = [];

    // Navigation
    public ICollection<CanvassQuotation> Quotations { get; private set; } = new List<CanvassQuotation>();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private CanvassRequest() { }

    public static CanvassRequest Create(string rivNumber, Guid purchaseRequestId, DateOnly returnDeadline)
    {
        return new CanvassRequest
        {
            Id = Guid.NewGuid(),
            RivNumber = rivNumber,
            PurchaseRequestId = purchaseRequestId,
            ReturnDeadline = returnDeadline,
            Status = CanvassRequestStatus.Open,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Award(Guid awardedSupplierId)
    {
        if (Status != CanvassRequestStatus.Open && Status != CanvassRequestStatus.Evaluated)
            throw new InvalidOperationException("Cannot award a canvass that is not Open or Evaluated.");

        Status = CanvassRequestStatus.Awarded;
        AwardedSupplierId = awardedSupplierId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CanvassRequestStatus.Awarded || Status == CanvassRequestStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a canvass request with status '{Status}'.");

        Status = CanvassRequestStatus.Cancelled;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Evaluate()
    {
        if (Status != CanvassRequestStatus.Open)
            throw new InvalidOperationException("Only Open canvass requests can be set to Evaluated.");

        Status = CanvassRequestStatus.Evaluated;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
