using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.ProcurementAcquisition.Domain.Canvass;

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

public sealed class CanvassQuotation : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
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
        string tenantId,
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
            TenantId = tenantId,
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

