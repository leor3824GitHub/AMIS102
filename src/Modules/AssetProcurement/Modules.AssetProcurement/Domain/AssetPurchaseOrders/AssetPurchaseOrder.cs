using FSH.Framework.Core.Domain;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;

namespace FSH.Modules.AssetProcurement.Domain.AssetPurchaseOrders;

public sealed class AssetPurchaseOrderLineItem
{
    public int ItemNo { get; private set; }
    public string Unit { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string? TechnicalSpecifications { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? PropertyClassHint { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;

    private AssetPurchaseOrderLineItem() { }

    public static AssetPurchaseOrderLineItem Create(
        int itemNo,
        string unit,
        string description,
        string? technicalSpecifications,
        string? brand,
        string? model,
        string? propertyClassHint,
        decimal quantity,
        decimal unitCost) =>
        new()
        {
            ItemNo = itemNo,
            Unit = unit,
            Description = description,
            TechnicalSpecifications = technicalSpecifications,
            Brand = brand,
            Model = model,
            PropertyClassHint = propertyClassHint,
            Quantity = quantity,
            UnitCost = unitCost
        };
}

public sealed class AssetPurchaseOrder : AggregateRoot<Guid>, IAuditableEntity
{
    public string PoNumber { get; private set; } = default!;
    public DateOnly PoDate { get; private set; }
    public Guid PurchaseRequestId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public string SupplierAddress { get; private set; } = default!;
    public string? SupplierTin { get; private set; }
    public AssetModeOfProcurement ModeOfProcurement { get; private set; }
    public string PlaceOfDelivery { get; private set; } = default!;
    public DateOnly? DateOfDelivery { get; private set; }
    public string DeliveryTerm { get; private set; } = default!;
    public string PaymentTerm { get; private set; } = default!;
    public string? FundCluster { get; private set; }
    public string? OblRequestNumber { get; private set; }
    public AssetPurchaseOrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] Version { get; set; } = [];

    private readonly List<AssetPurchaseOrderLineItem> _lineItems = [];
    public IReadOnlyList<AssetPurchaseOrderLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal TotalAmount => _lineItems.Sum(x => x.Amount);

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AssetPurchaseOrder() { }

    public static AssetPurchaseOrder Create(
        string poNumber,
        Guid purchaseRequestId,
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        AssetModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oblRequestNumber,
        IEnumerable<AssetPurchaseOrderLineItemRequest> lineItems)
    {
        var po = new AssetPurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNumber = poNumber,
            PoDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseRequestId = purchaseRequestId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            SupplierAddress = supplierAddress,
            SupplierTin = supplierTin,
            ModeOfProcurement = modeOfProcurement,
            PlaceOfDelivery = placeOfDelivery,
            DateOfDelivery = dateOfDelivery,
            DeliveryTerm = deliveryTerm,
            PaymentTerm = paymentTerm,
            FundCluster = fundCluster,
            OblRequestNumber = oblRequestNumber,
            Status = AssetPurchaseOrderStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
            po._lineItems.Add(AssetPurchaseOrderLineItem.Create(
                itemNo++, li.Unit, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint, li.Quantity, li.UnitCost));

        return po;
    }

    public void Update(
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        AssetModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oblRequestNumber,
        IEnumerable<AssetPurchaseOrderLineItemRequest> lineItems)
    {
        if (Status != AssetPurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase orders can be updated.");

        SupplierId = supplierId;
        SupplierName = supplierName;
        SupplierAddress = supplierAddress;
        SupplierTin = supplierTin;
        ModeOfProcurement = modeOfProcurement;
        PlaceOfDelivery = placeOfDelivery;
        DateOfDelivery = dateOfDelivery;
        DeliveryTerm = deliveryTerm;
        PaymentTerm = paymentTerm;
        FundCluster = fundCluster;
        OblRequestNumber = oblRequestNumber;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var li in lineItems)
            _lineItems.Add(AssetPurchaseOrderLineItem.Create(
                itemNo++, li.Unit, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint, li.Quantity, li.UnitCost));
    }

    public void Issue()
    {
        if (Status != AssetPurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase orders can be issued.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase order must have at least one line item.");

        Status = AssetPurchaseOrderStatus.Issued;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status is AssetPurchaseOrderStatus.Fulfilled or AssetPurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a purchase order with status '{Status}'.");

        Status = AssetPurchaseOrderStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
