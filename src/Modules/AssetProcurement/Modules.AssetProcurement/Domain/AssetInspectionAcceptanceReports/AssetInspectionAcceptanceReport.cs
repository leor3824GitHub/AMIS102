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

    /// <summary>Stock / Property No assigned by the operator at IAR time (SOP GS-PD26 column). Optional during Draft; required before acceptance.</summary>
    public string? StockPropertyNo { get; private set; }

    /// <summary>Per-line inspector decision. Defaults to <see cref="LineInspectionResult.Pending"/> for legacy lines.</summary>
    public LineInspectionResult InspectionResult { get; private set; } = LineInspectionResult.Pending;
    public DateTimeOffset? InspectedOnUtc { get; private set; }
    public Guid? InspectedById { get; private set; }

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
        string? inspectionRemarks,
        string? stockPropertyNo) =>
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
            StockPropertyNo = string.IsNullOrWhiteSpace(stockPropertyNo) ? null : stockPropertyNo.Trim()
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

    internal AssetIARLineItem CloneAsSingleUnit(int newItemNo) =>
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
            InspectionResult = InspectionResult,
            InspectedById = InspectedById,
            InspectedOnUtc = InspectedOnUtc
        };

    internal void SetQuantity(decimal qty) => Quantity = qty;
}

public sealed class AssetInspectionAcceptanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
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
    public AssetIARStatus Status { get; private set; }
    public string? Remarks { get; private set; }
    public string? RejectionReason { get; private set; }
    public byte[] Version { get; set; } = [];

    public DateTimeOffset? SubmittedForInspectionOnUtc { get; private set; }
    public DateTimeOffset? InspectedOnUtc { get; private set; }
    public DateTimeOffset? AcceptedOnUtc { get; private set; }
    public DateTimeOffset? CancelledOnUtc { get; private set; }

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
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks,
                li.StockPropertyNo));

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
                li.Unit, li.Quantity, li.UnitCost, li.InspectionRemarks,
                li.StockPropertyNo));
    }

    /// <summary>Property Custodian sends the IAR to the assigned inspector. Header becomes locked for editing.</summary>
    public void SubmitForInspection()
    {
        if (Status != AssetIARStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be submitted for inspection.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("IAR must have at least one line item before submission.");
        if (InspectedById == Guid.Empty)
            throw new InvalidOperationException("An inspector must be assigned before submission.");

        Status = AssetIARStatus.PendingInspection;
        SubmittedForInspectionOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = SubmittedForInspectionOnUtc;
    }

    /// <summary>Property Custodian replaces the assigned inspector while the IAR is awaiting inspection.</summary>
    public void ReassignInspector(Guid newInspectorId)
    {
        if (Status != AssetIARStatus.PendingInspection)
            throw new InvalidOperationException("Only IARs awaiting inspection can have their inspector reassigned.");
        if (newInspectorId == Guid.Empty)
            throw new InvalidOperationException("New inspector is required.");

        InspectedById = newInspectorId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Inspector records pass/fail per line. Caller MUST verify <paramref name="actorId"/> matches <see cref="InspectedById"/>.</summary>
    public void RecordInspection(Guid actorId, IEnumerable<LineInspectionDecision> decisions)
    {
        if (Status != AssetIARStatus.PendingInspection)
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

        Status = AssetIARStatus.Inspected;
        InspectedOnUtc = whenUtc;
        LastModifiedOnUtc = whenUtc;
    }

    /// <summary>Property Custodian assigns a Property No to a Passed line during the Acceptance stage.</summary>
    public void AssignPropertyNo(int itemNo, string propertyNo)
    {
        if (Status != AssetIARStatus.Inspected)
            throw new InvalidOperationException("Property numbers can only be assigned after inspection.");

        var line = _lineItems.FirstOrDefault(li => li.ItemNo == itemNo)
            ?? throw new KeyNotFoundException($"Line item {itemNo} not found.");

        line.AssignPropertyNo(propertyNo);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Splits a Passed line with Qty &gt; 1 into N lines of Qty = 1, copying inspection result. Implements NFA "one line per physical unit".</summary>
    public void ExpandLineByQuantity(int itemNo)
    {
        if (Status != AssetIARStatus.Inspected)
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

    public void Accept()
    {
        // Allowed from Draft (legacy direct path) or Inspected (new staged path).
        if (Status != AssetIARStatus.Draft && Status != AssetIARStatus.Inspected)
            throw new InvalidOperationException("IAR can only be accepted from Draft or Inspected status.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("IAR must have at least one line item.");

        var missing = _lineItems
            .Where(li => li.InspectionResult != LineInspectionResult.Rejected
                         && string.IsNullOrWhiteSpace(li.StockPropertyNo))
            .Select(li => li.ItemNo)
            .ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Cannot accept IAR: Stock/Property No is required on every non-rejected line. Missing on item(s): {string.Join(", ", missing)}.");

        Status = AssetIARStatus.Accepted;
        AcceptedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = AcceptedOnUtc;
    }

    public void Reject(string reason)
    {
        if (Status != AssetIARStatus.Draft)
            throw new InvalidOperationException("Only Draft IARs can be rejected.");

        Status = AssetIARStatus.Rejected;
        RejectionReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Property Custodian abandons an IAR before acceptance. Allowed from Draft, PendingInspection, or Inspected.</summary>
    public void Cancel()
    {
        if (Status is not (AssetIARStatus.Draft or AssetIARStatus.PendingInspection or AssetIARStatus.Inspected))
            throw new InvalidOperationException("Only IARs that have not yet been accepted, rejected, or cancelled can be cancelled.");

        Status = AssetIARStatus.Cancelled;
        CancelledOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = CancelledOnUtc;
    }
}
