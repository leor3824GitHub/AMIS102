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
    Guid? CatalogItemId = null);

public sealed class PurchaseOrderLineItem
{
    public int ItemNo { get; private set; }
    public string? StockNumber { get; private set; }
    public string Unit { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal Amount => Quantity * UnitCost;

    /// <summary>Snapshot of the PR-side catalog reference. Carries forward to IAR so the accepted asset
    /// can be classified against its <c>PropertyItemCatalog</c> (which holds the authoritative UACS).</summary>
    public Guid? CatalogItemId { get; private set; }

    private PurchaseOrderLineItem() { }

    public static PurchaseOrderLineItem Create(
        int itemNo,
        string? stockNumber,
        string unit,
        string description,
        decimal quantity,
        decimal unitCost,
        Guid? catalogItemId = null)
    {
        return new PurchaseOrderLineItem
        {
            ItemNo = itemNo,
            StockNumber = stockNumber,
            Unit = unit,
            Description = description,
            Quantity = quantity,
            UnitCost = unitCost,
            CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId
        };
    }

    public void Update(
        string? stockNumber,
        string unit,
        string description,
        decimal quantity,
        decimal unitCost,
        Guid? catalogItemId = null)
    {
        StockNumber = stockNumber;
        Unit = unit;
        Description = description;
        Quantity = quantity;
        UnitCost = unitCost;
        CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId;
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

    // "Funds Available" — Accountant signs and assigns UACS codes
    public Guid? FundsAvailableCertifiedById { get; private set; }
    public string? FundsAvailableCertifiedByName { get; private set; }
    public DateTimeOffset? FundsAvailableCertifiedOnUtc { get; private set; }

    // "Approved" — Authorized Official who issued the PO (printed in the bottom-right signature block)
    public Guid? IssuedById { get; private set; }
    public string? IssuedByName { get; private set; }
    public DateTimeOffset? IssuedOnUtc { get; private set; }

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
                li.CatalogItemId));
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
                li.CatalogItemId));
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
    /// Accountant signs "Funds Available" and optionally captures the ORS/BURS reference and Fund Cluster.
    /// Moves PendingFundsAvailable → PendingApproval (awaiting Issue). UACS object codes are certified on the
    /// source PR (and held on the catalog), not on the supplier-facing PO.
    /// </summary>
    public void CertifyFundsAvailable(
        Guid certifiedById,
        string certifiedByName,
        string? oursBursNumber,
        DateOnly? oursBursDate,
        string? fundCluster)
    {
        if (Status != PurchaseOrderStatus.PendingFundsAvailable)
            throw new InvalidOperationException("Funds Available can only be certified on POs awaiting Accountant review.");
        if (string.IsNullOrWhiteSpace(certifiedByName))
            throw new InvalidOperationException("Accountant name is required.");

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

    /// <summary>
    /// Authorized Official approves and issues the PO. Captures who issued it for the
    /// "Very truly yours" signature block on the printed PO. Moves PendingApproval → Issued.
    /// </summary>
    public void Issue(Guid issuedById, string? issuedByName)
    {
        if (Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("Only POs that have passed Funds Available certification can be issued.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase order must have at least one line item.");

        IssuedById = issuedById;
        IssuedByName = issuedByName;
        IssuedOnUtc = DateTimeOffset.UtcNow;

        Status = PurchaseOrderStatus.Issued;
        LastModifiedOnUtc = IssuedOnUtc.Value;
    }

    /// <summary>
    /// Records cumulative accepted delivery against this PO and advances its status.
    /// <paramref name="totalAcceptedQuantity"/> is the running total of non-rejected IAR line
    /// quantities across all accepted IARs for this PO. Matching is done on total quantity
    /// (ordered vs. accepted), not per line item — IARs carry no PO-line key to join on.
    /// No-ops unless the PO is currently Issued or PartiallyDelivered, so it is safe to call
    /// again on an already-Fulfilled or Cancelled PO.
    /// </summary>
    public void RecordDelivery(decimal totalAcceptedQuantity)
    {
        if (Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyDelivered))
            return;

        var orderedQuantity = _lineItems.Sum(x => x.Quantity);

        if (totalAcceptedQuantity >= orderedQuantity)
            Status = PurchaseOrderStatus.Fulfilled;
        else if (totalAcceptedQuantity > 0)
            Status = PurchaseOrderStatus.PartiallyDelivered;

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

