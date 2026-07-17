using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;

public sealed class InspectionAcceptanceReportLineItem
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

    /// <summary>Stock / Property No assigned by the operator at IAR time (SOP GS-PD26 column). Optional during Draft; required before acceptance.</summary>
    public string? StockPropertyNo { get; private set; }

    /// <summary>For Supply IARs: the StockNo of the screened Expendable product (copied from the PO line).
    /// Distinct from <see cref="StockPropertyNo"/> (the asset property number). Used by the Expendable module
    /// to match the accepted line to a product. Null for Asset IARs.</summary>
    public string? StockNumber { get; private set; }

    /// <summary>Snapshot of the PR/PO catalog reference. Carries forward into PPERR and AssetRegistry.</summary>
    public Guid? CatalogItemId { get; private set; }

    /// <summary>Snapshot of the Accountant-assigned UACS Object Code copied from the source PR/PO line.</summary>
    public string? UacsObjectCode { get; private set; }

    /// <summary>Per-line inspector decision. Defaults to <see cref="LineInspectionResult.Pending"/> for legacy lines.</summary>
    public LineInspectionResult InspectionResult { get; private set; } = LineInspectionResult.Pending;
    public DateTimeOffset? InspectedOnUtc { get; private set; }
    public Guid? InspectedById { get; private set; }

    private InspectionAcceptanceReportLineItem() { }

    public static InspectionAcceptanceReportLineItem Create(
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
        string? inspectionRemarks,
        string? stockPropertyNo,
        Guid? catalogItemId = null,
        string? uacsObjectCode = null,
        string? stockNumber = null) =>
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
            InspectionRemarks = inspectionRemarks,
            StockPropertyNo = string.IsNullOrWhiteSpace(stockPropertyNo) ? null : stockPropertyNo.Trim(),
            CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId,
            UacsObjectCode = string.IsNullOrWhiteSpace(uacsObjectCode) ? null : uacsObjectCode.Trim(),
            StockNumber = string.IsNullOrWhiteSpace(stockNumber) ? null : stockNumber.Trim()
        };

    internal void RecordInspection(LineInspectionResult result, string? remarks, Guid inspectorId, DateTimeOffset whenUtc)
    {
        if (result == LineInspectionResult.Pending)
            throw new InvalidOperationException("Inspection result must be Passed or Rejected.");
        if (result == LineInspectionResult.Rejected && string.IsNullOrWhiteSpace(remarks))
            throw new InvalidOperationException($"Item {ItemNo}: remarks are required when rejecting a line.");

        InspectionResult = result;
        InspectionRemarks = string.IsNullOrWhiteSpace(remarks) ? InspectionRemarks : remarks;
        InspectedById = inspectorId;
        InspectedOnUtc = whenUtc;
    }

    internal void AssignPropertyNo(string propertyNo)
    {
        if (InspectionResult == LineInspectionResult.Rejected)
            throw new InvalidOperationException($"Item {ItemNo}: cannot assign Property No to a rejected line.");
        if (string.IsNullOrWhiteSpace(propertyNo))
            throw new InvalidOperationException($"Item {ItemNo}: Property No is required.");

        StockPropertyNo = propertyNo.Trim().ToUpperInvariant();
    }

    internal void Renumber(int newItemNo) => ItemNo = newItemNo;

    internal InspectionAcceptanceReportLineItem CloneAsSingleUnit(int newItemNo) =>
        new()
        {
            ItemNo = newItemNo,
            Description = Description,
            TechnicalSpecifications = TechnicalSpecifications,
            Brand = Brand,
            Model = Model,
            SerialNo = SerialNo,
            PropertyClassHint = PropertyClassHint,
            Unit = Unit,
            Quantity = 1m,
            UnitCost = UnitCost,
            InspectionRemarks = InspectionRemarks,
            StockPropertyNo = null,
            CatalogItemId = CatalogItemId,
            UacsObjectCode = UacsObjectCode,
            StockNumber = StockNumber,
            InspectionResult = InspectionResult,
            InspectedById = InspectedById,
            InspectedOnUtc = InspectedOnUtc
        };

    internal void SetQuantity(decimal qty) => Quantity = qty;
}

public sealed class InspectionAcceptanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISignedCopyHolder
{
    public string TenantId { get; private set; } = default!;
    public string IarNumber { get; private set; } = default!;
    public DateOnly IarDate { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = default!;

    /// <summary>The inspector assigned to this IAR by the Property Custodian. Only this user can record inspection.</summary>
    public Guid InspectedById { get; private set; }

    /// <summary>The Property Custodian (Supply Officer) who owns this IAR.</summary>
    public Guid ReceivedById { get; private set; }

    public string? DeliveryReceiptNo { get; private set; }
    public DateOnly? DeliveryDate { get; private set; }
    public InspectionAcceptanceReportStatus Status { get; private set; }

    /// <summary>Asset vs Supply — copied from the PO at creation (not operator-chosen). Supply IARs skip the
    /// one-line-per-unit + Property-No acceptance rules and publish <see cref="SupplyIARAcceptedEvent"/>.</summary>
    public ProcurementCategory Category { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset? SubmittedForInspectionOnUtc { get; private set; }
    public DateTimeOffset? InspectedOnUtc { get; private set; }
    public DateTimeOffset? AcceptedOnUtc { get; private set; }
    public DateTimeOffset? CancelledOnUtc { get; private set; }

    /// <summary>The authenticated user who performed the acceptance. Frozen at the moment of the action
    /// (faithful-reprint plan) — distinct from <see cref="ReceivedById"/>, the custodian assigned at creation.</summary>
    public Guid? AcceptedById { get; private set; }
    public string? AcceptedByName { get; private set; }
    public string? AcceptedByDesignation { get; private set; }

    private readonly List<InspectionAcceptanceReportLineItem> _lineItems = [];
    public IReadOnlyList<InspectionAcceptanceReportLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal TotalAmount => _lineItems.Sum(x => x.Amount);

    /// <summary>The uploaded wet-signed copy of this document of record; null until one is uploaded.</summary>
    public SignedCopy? SignedCopy { get; private set; }

    /// <summary>Attaches or replaces the signed copy (one current copy per document).</summary>
    public void SetSignedCopy(SignedCopy copy) => SignedCopy = copy;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private InspectionAcceptanceReport() { }

    public static InspectionAcceptanceReport Create(
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
        IEnumerable<InspectionAcceptanceReportLineItemRequest> lineItems,
        ProcurementCategory category = ProcurementCategory.Asset)
    {
        var iar = new InspectionAcceptanceReport
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
            Status = InspectionAcceptanceReportStatus.Draft,
            Category = category,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
            iar._lineItems.Add(InspectionAcceptanceReportLineItem.Create(
                itemNo++, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks,
                li.StockPropertyNo, li.CatalogItemId, li.UacsObjectCode, li.StockNumber));

        return iar;
    }

    public void Update(
        Guid inspectedById,
        Guid receivedById,
        string? deliveryReceiptNo,
        DateOnly? deliveryDate,
        string? remarks,
        IEnumerable<InspectionAcceptanceReportLineItemRequest> lineItems)
    {
        if (Status != InspectionAcceptanceReportStatus.Draft)
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
            _lineItems.Add(InspectionAcceptanceReportLineItem.Create(
                itemNo++, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks,
                li.StockPropertyNo, li.CatalogItemId, li.UacsObjectCode, li.StockNumber));
    }

    /// <summary>Property Custodian sends the IAR to the assigned inspector. Header becomes locked for editing.</summary>
    public void SubmitForInspection()
    {
        if (Status != InspectionAcceptanceReportStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be submitted for inspection.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("IAR must have at least one line item before submission.");
        if (InspectedById == Guid.Empty)
            throw new InvalidOperationException("An inspector must be assigned before submission.");

        Status = InspectionAcceptanceReportStatus.PendingInspection;
        SubmittedForInspectionOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = SubmittedForInspectionOnUtc;
    }

    /// <summary>Property Custodian replaces the assigned inspector while the IAR is awaiting inspection.</summary>
    public void ReassignInspector(Guid newInspectorId)
    {
        if (Status != InspectionAcceptanceReportStatus.PendingInspection)
            throw new InvalidOperationException("Only IARs awaiting inspection can have their inspector reassigned.");
        if (newInspectorId == Guid.Empty)
            throw new InvalidOperationException("New inspector is required.");

        InspectedById = newInspectorId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Inspector records pass/fail per line. Caller MUST verify <paramref name="actorId"/> matches <see cref="InspectedById"/>.</summary>
    public void RecordInspection(Guid actorId, IEnumerable<LineInspectionDecision> decisions)
    {
        if (Status != InspectionAcceptanceReportStatus.PendingInspection)
            throw new InvalidOperationException("Inspection can only be recorded on IARs awaiting inspection.");
        if (actorId != InspectedById)
            throw new UnauthorizedAccessException("Only the assigned inspector can record inspection on this IAR.");

        var byItemNo = decisions.ToDictionary(d => d.ItemNo);
        var missing = _lineItems
            .Where(li => !byItemNo.ContainsKey(li.ItemNo) || byItemNo[li.ItemNo].Result == LineInspectionResult.Pending)
            .Select(li => li.ItemNo)
            .ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Every line must have a Passed/Rejected decision. Missing on item(s): {string.Join(", ", missing)}.");

        var whenUtc = DateTimeOffset.UtcNow;
        foreach (var li in _lineItems)
        {
            var d = byItemNo[li.ItemNo];
            li.RecordInspection(d.Result, d.Remarks, actorId, whenUtc);
        }

        Status = InspectionAcceptanceReportStatus.Inspected;
        InspectedOnUtc = whenUtc;
        LastModifiedOnUtc = whenUtc;
    }

    /// <summary>Property Custodian assigns a Property No to a Passed line during the Acceptance stage.</summary>
    public void AssignPropertyNo(int itemNo, string propertyNo)
    {
        if (Status != InspectionAcceptanceReportStatus.Inspected)
            throw new InvalidOperationException("Property numbers can only be assigned after inspection.");

        var line = _lineItems.FirstOrDefault(li => li.ItemNo == itemNo)
            ?? throw new KeyNotFoundException($"Line item {itemNo} not found.");

        line.AssignPropertyNo(propertyNo);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Splits a Passed line with Qty &gt; 1 into N lines of Qty = 1, copying inspection result. Implements NFA "one line per physical unit".</summary>
    public void ExpandLineByQuantity(int itemNo)
    {
        if (Status != InspectionAcceptanceReportStatus.Inspected)
            throw new InvalidOperationException("Lines can only be expanded after inspection.");

        var line = _lineItems.FirstOrDefault(li => li.ItemNo == itemNo)
            ?? throw new KeyNotFoundException($"Line item {itemNo} not found.");

        if (line.InspectionResult != LineInspectionResult.Passed)
            throw new InvalidOperationException($"Item {itemNo}: only Passed lines can be expanded.");
        if (line.Quantity <= 1m)
            throw new InvalidOperationException($"Item {itemNo}: quantity is already 1 or less.");

        var copies = (int)Math.Floor(line.Quantity) - 1;
        line.SetQuantity(1m);

        var insertIndex = _lineItems.IndexOf(line) + 1;
        for (var i = 0; i < copies; i++)
        {
            _lineItems.Insert(insertIndex + i, line.CloneAsSingleUnit(0)); // ItemNo set by Renumber below
        }

        var n = 1;
        foreach (var li in _lineItems) li.Renumber(n++);

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Property Custodian accepts the inspected IAR. Captures the authenticated actor (name + designation
    /// frozen at this moment) for the acceptance signature block. Moves Inspected → Accepted.
    /// </summary>
    public void Accept(Guid acceptedById, string? acceptedByName, string? acceptedByDesignation = null)
    {
        if (Status != InspectionAcceptanceReportStatus.Inspected)
            throw new InvalidOperationException("IAR can only be accepted after inspection is complete.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("IAR must have at least one line item.");

        // Asset (PPE/SE) IARs are accountable property: NFA policy requires one line per physical unit and a
        // Property No per unit. Supply (expendable) IARs are consumables — quantities stay bulk and need no
        // Property No, so these two checks are Asset-only.
        if (Category == ProcurementCategory.Asset)
        {
            var nonUnit = _lineItems
                .Where(li => li.InspectionResult != LineInspectionResult.Rejected && li.Quantity != 1m)
                .Select(li => li.ItemNo)
                .ToList();
            if (nonUnit.Count > 0)
                throw new InvalidOperationException(
                    $"Cannot accept IAR: every non-rejected line must have quantity 1 (one line per physical unit). Expand quantity first. Offending item(s): {string.Join(", ", nonUnit)}.");

            var missing = _lineItems
                .Where(li => li.InspectionResult != LineInspectionResult.Rejected
                             && string.IsNullOrWhiteSpace(li.StockPropertyNo))
                .Select(li => li.ItemNo)
                .ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"Cannot accept IAR: Stock/Property No is required on every non-rejected line. Missing on item(s): {string.Join(", ", missing)}.");
        }

        Status = InspectionAcceptanceReportStatus.Accepted;
        AcceptedOnUtc = DateTimeOffset.UtcNow;
        AcceptedById = acceptedById;
        AcceptedByName = acceptedByName;
        AcceptedByDesignation = acceptedByDesignation;
        LastModifiedOnUtc = AcceptedOnUtc;
    }

    /// <summary>Property Custodian abandons an IAR before acceptance. Allowed from Draft, PendingInspection, or Inspected.</summary>
    public void Cancel()
    {
        if (Status is not (InspectionAcceptanceReportStatus.Draft or InspectionAcceptanceReportStatus.PendingInspection or InspectionAcceptanceReportStatus.Inspected))
            throw new InvalidOperationException("Only IARs that have not yet been accepted or cancelled can be cancelled.");

        Status = InspectionAcceptanceReportStatus.Cancelled;
        CancelledOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = CancelledOnUtc;
    }
}
