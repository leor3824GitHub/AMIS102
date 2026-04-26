using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

public sealed class TangibleItem : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid ItemId { get; private set; }
    public PropertyItemCatalog Item { get; private set; } = default!;
    public string PropertyNo { get; private set; } = default!;
    public string PropertyClass { get; private set; } = default!;
    public string CategoryCode { get; private set; } = default!;
    public DateOnly AcquisitionDate { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? Remarks { get; private set; }

    /// <summary>Set when this item is received on a TangibleInventory. Null = pending receipt.</summary>
    public Guid? TangibleInventoryItemId { get; private set; }

    /// <summary>Optional reference to the Purchase Order that sourced this item. Null for donations/transfers.</summary>
    public Guid? PurchaseOrderId { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static TangibleItem Create(
        string tenantId,
        Guid itemId,
        string propertyNo,
        string propertyClass,
        string categoryCode,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost,
        string? remarks,
        Guid? purchaseOrderId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ItemId = itemId,
            PropertyNo = propertyNo,
            PropertyClass = propertyClass,
            CategoryCode = categoryCode,
            AcquisitionDate = acquisitionDate,
            Quantity = quantity,
            UnitCost = unitCost,
            Remarks = remarks,
            PurchaseOrderId = purchaseOrderId,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };

    public void LinkToInventory(Guid tangibleInventoryItemId) => TangibleInventoryItemId = tangibleInventoryItemId;

    public void Update(DateOnly acquisitionDate, int quantity, decimal unitCost, string? remarks, Guid? purchaseOrderId)
    {
        AcquisitionDate = acquisitionDate;
        Quantity = quantity;
        UnitCost = unitCost;
        Remarks = remarks;
        PurchaseOrderId = purchaseOrderId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
