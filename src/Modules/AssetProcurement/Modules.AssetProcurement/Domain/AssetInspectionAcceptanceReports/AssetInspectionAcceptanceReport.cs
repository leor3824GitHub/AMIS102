using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;

public sealed class AssetIARLineItem
{
    public int ItemNo { get; private set; }
    public string Description { get; private set; } = default!;
    public string? TechnicalSpecifications { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNo { get; private set; }
    public string? PropertyClassHint { get; private set; }
    public string Unit { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;
    public string? InspectionRemarks { get; private set; }

    private AssetIARLineItem() { }

    public static AssetIARLineItem Create(
        int itemNo,
        string description,
        string? technicalSpecifications,
        string? brand,
        string? model,
        string? serialNo,
        string? propertyClassHint,
        string unit,
        decimal quantity,
        decimal unitCost,
        string? inspectionRemarks) =>
        new()
        {
            ItemNo = itemNo,
            Description = description,
            TechnicalSpecifications = technicalSpecifications,
            Brand = brand,
            Model = model,
            SerialNo = serialNo,
            PropertyClassHint = propertyClassHint,
            Unit = unit,
            Quantity = quantity,
            UnitCost = unitCost,
            InspectionRemarks = inspectionRemarks
        };
}

public sealed class AssetInspectionAcceptanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string IarNumber { get; private set; } = default!;
    public DateOnly IarDate { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public Guid InspectedById { get; private set; }
    public Guid ReceivedById { get; private set; }
    public string? DeliveryReceiptNo { get; private set; }
    public DateOnly? DeliveryDate { get; private set; }
    public AssetIARStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public string? RejectionReason { get; private set; }
    public byte[] Version { get; set; } = [];

    private readonly List<AssetIARLineItem> _lineItems = [];
    public IReadOnlyList<AssetIARLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal TotalAmount => _lineItems.Sum(x => x.Amount);

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AssetInspectionAcceptanceReport() { }

    public static AssetInspectionAcceptanceReport Create(
        string tenantId,
        string iarNumber,
        Guid purchaseOrderId,
        Guid supplierId,
        string supplierName,
        Guid inspectedById,
        Guid receivedById,
        string? deliveryReceiptNo,
        DateOnly? deliveryDate,
        string? remarks,
        IEnumerable<AssetIARLineItemRequest> lineItems)
    {
        var iar = new AssetInspectionAcceptanceReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IarNumber = iarNumber,
            IarDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseOrderId = purchaseOrderId,
            SupplierId = supplierId,
            SupplierName = supplierName,
            InspectedById = inspectedById,
            ReceivedById = receivedById,
            DeliveryReceiptNo = deliveryReceiptNo,
            DeliveryDate = deliveryDate,
            Remarks = remarks,
            Status = AssetIARStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
            iar._lineItems.Add(AssetIARLineItem.Create(
                itemNo++, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks));

        return iar;
    }

    public void Update(
        Guid inspectedById,
        Guid receivedById,
        string? deliveryReceiptNo,
        DateOnly? deliveryDate,
        string? remarks,
        IEnumerable<AssetIARLineItemRequest> lineItems)
    {
        if (Status != AssetIARStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be updated.");

        InspectedById = inspectedById;
        ReceivedById = receivedById;
        DeliveryReceiptNo = deliveryReceiptNo;
        DeliveryDate = deliveryDate;
        Remarks = remarks;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var li in lineItems)
            _lineItems.Add(AssetIARLineItem.Create(
                itemNo++, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks));
    }

    public void Accept()
    {
        if (Status != AssetIARStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be accepted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("IAR must have at least one line item.");

        Status = AssetIARStatus.Accepted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != AssetIARStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be rejected.");

        Status = AssetIARStatus.Rejected;
        RejectionReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

