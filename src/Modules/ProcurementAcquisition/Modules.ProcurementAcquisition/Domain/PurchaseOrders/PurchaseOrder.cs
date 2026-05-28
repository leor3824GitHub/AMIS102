using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;

/// <summary>Domain-internal carrier for PO line item input. Mirrors <c>PurchaseRequestLineItemData</c>.</summary>
public readonly record struct PurchaseOrderLineItemData(
    string? StockNumber,
    string Unit,
    string Description,
    decimal Quantity,
    decimal UnitCost,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null);

public sealed class PurchaseOrderLineItem
{
    public int ItemNo { get; private set; }
    public string? StockNumber { get; private set; }
    public string Unit { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;

    /// <summary>Snapshot of the PR-side catalog reference. Carries forward to IAR / PPERR.</summary>
    public Guid? CatalogItemId { get; private set; }

    /// <summary>Snapshot of the Accountant-assigned UACS Object Code. Carries forward to IAR / PPERR.</summary>
    public string? UacsObjectCode { get; private set; }

    private PurchaseOrderLineItem() { }

    public static PurchaseOrderLineItem Create(
        int itemNo,
        string? stockNumber,
        string unit,
        string description,
        decimal quantity,
        decimal unitCost,
        Guid? catalogItemId = null,
        string? uacsObjectCode = null)
    {
        return new PurchaseOrderLineItem
        {
            ItemNo = itemNo,
            StockNumber = stockNumber,
            Unit = unit,
            Description = description,
            Quantity = quantity,
            UnitCost = unitCost,
            CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId,
            UacsObjectCode = string.IsNullOrWhiteSpace(uacsObjectCode) ? null : uacsObjectCode.Trim()
        };
    }

    public void Update(
        string? stockNumber,
        string unit,
        string description,
        decimal quantity,
        decimal unitCost,
        Guid? catalogItemId = null,
        string? uacsObjectCode = null)
    {
        StockNumber = stockNumber;
        Unit = unit;
        Description = description;
        Quantity = quantity;
        UnitCost = unitCost;
        CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId;
        UacsObjectCode = string.IsNullOrWhiteSpace(uacsObjectCode) ? null : uacsObjectCode.Trim();
    }

    internal void AssignUacs(string uacsObjectCode)
    {
        if (string.IsNullOrWhiteSpace(uacsObjectCode))
            throw new InvalidOperationException($"Item {ItemNo}: UACS Object Code is required.");
        UacsObjectCode = uacsObjectCode.Trim();
    }
}

public sealed class PurchaseOrder : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string PoNumber { get; private set; } = default!;
    public DateOnly PoDate { get; private set; }
    public Guid PurchaseRequestId { get; private set; }
    public Guid? CanvassRequestId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;
    public string SupplierAddress { get; private set; } = default!;
    public string? SupplierTin { get; private set; }
    public ModeOfProcurement ModeOfProcurement { get; private set; }
    public string PlaceOfDelivery { get; private set; } = default!;
    public DateOnly? DateOfDelivery { get; private set; }
    public string DeliveryTerm { get; private set; } = default!;
    public string PaymentTerm { get; private set; } = default!;
    public string? FundCluster { get; private set; }
    public string? OursBursNumber { get; private set; }
    public DateOnly? OursBursDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] Version { get; set; } = [];

    // "Funds Available" — Accountant signs and assigns UACS codes
    public Guid? FundsAvailableCertifiedById { get; private set; }
    public string? FundsAvailableCertifiedByName { get; private set; }
    public DateTimeOffset? FundsAvailableCertifiedOnUtc { get; private set; }

    private readonly List<PurchaseOrderLineItem> _lineItems = [];
    public IReadOnlyList<PurchaseOrderLineItem> LineItems => _lineItems.AsReadOnly();

    public decimal TotalAmount => _lineItems.Sum(x => x.Amount);

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        string tenantId,
        string poNumber,
        Guid purchaseRequestId,
        Guid? canvassRequestId,
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        ModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oursBursNumber,
        IEnumerable<PurchaseOrderLineItemData> lineItems)
    {
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PoNumber = poNumber,
            PoDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PurchaseRequestId = purchaseRequestId,
            CanvassRequestId = canvassRequestId,
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
            OursBursNumber = oursBursNumber,
            OursBursDate = null,
            Status = PurchaseOrderStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
        {
            po._lineItems.Add(PurchaseOrderLineItem.Create(
                itemNo++, li.StockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost,
                li.CatalogItemId, li.UacsObjectCode));
        }

        return po;
    }

    public void Update(
        Guid supplierId,
        string supplierName,
        string supplierAddress,
        string? supplierTin,
        ModeOfProcurement modeOfProcurement,
        string placeOfDelivery,
        DateOnly? dateOfDelivery,
        string deliveryTerm,
        string paymentTerm,
        string? fundCluster,
        string? oursBursNumber,
        IEnumerable<PurchaseOrderLineItemData> lineItems)
    {
        if (Status != PurchaseOrderStatus.Draft)
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
        OursBursNumber = oursBursNumber;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var li in lineItems)
        {
            _lineItems.Add(PurchaseOrderLineItem.Create(
                itemNo++, li.StockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost,
                li.CatalogItemId, li.UacsObjectCode));
        }
    }

    /// <summary>
    /// Buyer submits the PO. Moves Draft → PendingFundsAvailable (awaiting Accountant).
    /// </summary>
    public void Submit()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase orders can be submitted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase order must have at least one line item.");

        Status = PurchaseOrderStatus.PendingFundsAvailable;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Accountant signs "Funds Available", assigns a UACS Object Code per line item, and optionally
    /// captures ORS/BURS reference. Moves PendingFundsAvailable → PendingApproval (awaiting Issue).
    /// </summary>
    public void CertifyFundsAvailable(
        Guid certifiedById,
        string certifiedByName,
        IReadOnlyDictionary<int, string> uacsByItemNo,
        string? oursBursNumber,
        DateOnly? oursBursDate,
        string? fundCluster)
    {
        if (Status != PurchaseOrderStatus.PendingFundsAvailable)
            throw new InvalidOperationException("Funds Available can only be certified on POs awaiting Accountant review.");
        if (string.IsNullOrWhiteSpace(certifiedByName))
            throw new InvalidOperationException("Accountant name is required.");
        ArgumentNullException.ThrowIfNull(uacsByItemNo);

        var missing = _lineItems
            .Where(li => !uacsByItemNo.TryGetValue(li.ItemNo, out var u) || string.IsNullOrWhiteSpace(u))
            .Select(li => li.ItemNo)
            .ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Every line item requires a UACS Object Code. Missing on item(s): {string.Join(", ", missing)}.");

        foreach (var li in _lineItems)
            li.AssignUacs(uacsByItemNo[li.ItemNo]);

        if (!string.IsNullOrWhiteSpace(oursBursNumber))
        {
            OursBursNumber = oursBursNumber;
            OursBursDate = oursBursDate;
        }

        if (!string.IsNullOrWhiteSpace(fundCluster))
            FundCluster = fundCluster;

        FundsAvailableCertifiedById = certifiedById;
        FundsAvailableCertifiedByName = certifiedByName;
        FundsAvailableCertifiedOnUtc = DateTimeOffset.UtcNow;

        Status = PurchaseOrderStatus.PendingApproval;
        LastModifiedOnUtc = FundsAvailableCertifiedOnUtc;
    }

    public void Issue()
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("Only POs that have passed Funds Available certification can be issued.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase order must have at least one line item.");

        Status = PurchaseOrderStatus.Issued;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == PurchaseOrderStatus.Fulfilled || Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a purchase order with status '{Status}'.");

        Status = PurchaseOrderStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

