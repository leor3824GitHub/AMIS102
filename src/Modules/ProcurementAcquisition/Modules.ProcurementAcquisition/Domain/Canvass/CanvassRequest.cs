using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Domain.Canvass;

/// <summary>Domain-internal carrier for covered PR line input. The create handler maps the selected PR lines to this before calling <see cref="CanvassRequest.Create"/>.</summary>
public readonly record struct CanvassRequestLineItemData(
    int PrItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal EstimatedUnitCost,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null);

/// <summary>
/// Snapshot of one Purchase Request line item covered by this canvass. Captured at canvass creation
/// (PR lines are immutable once Approved) so the RFQ report and quotation entry are self-contained.
/// <see cref="PrItemNo"/> is the partition key: each PR line belongs to at most one non-cancelled canvass.
/// </summary>
public sealed class CanvassRequestLineItem
{
    public int PrItemNo { get; private set; }
    public string Description { get; private set; } = default!;
    public string Unit { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public Guid? CatalogItemId { get; private set; }
    public string? UacsObjectCode { get; private set; }
    public decimal EstimatedTotalCost => Quantity * EstimatedUnitCost;

    private CanvassRequestLineItem() { }

    public static CanvassRequestLineItem Create(
        int prItemNo,
        string description,
        string unit,
        decimal quantity,
        decimal estimatedUnitCost,
        Guid? catalogItemId = null,
        string? uacsObjectCode = null)
    {
        return new CanvassRequestLineItem
        {
            PrItemNo = prItemNo,
            Description = description,
            Unit = unit,
            Quantity = quantity,
            EstimatedUnitCost = estimatedUnitCost,
            CatalogItemId = catalogItemId == Guid.Empty ? null : catalogItemId,
            UacsObjectCode = string.IsNullOrWhiteSpace(uacsObjectCode) ? null : uacsObjectCode.Trim()
        };
    }
}

/// <summary>
/// One ROPC committee signatory frozen onto the canvass at award time, so the printed Abstract of
/// Canvass stays faithful to who was on the committee when it was awarded — even if the configured
/// <c>ReportSignatories</c> table changes later. <see cref="SortOrder"/> mirrors the report slot
/// (1–4 = Members, 5 = Vice-Chair, 6 = Chair).
/// </summary>
public sealed class CanvassAwardSignatory
{
    public int SortOrder { get; private set; }
    public string Name { get; private set; } = default!;
    public string Role { get; private set; } = default!;

    private CanvassAwardSignatory() { }

    public static CanvassAwardSignatory Create(int sortOrder, string name, string role) =>
        new() { SortOrder = sortOrder, Name = name ?? string.Empty, Role = role ?? string.Empty };
}

public sealed class CanvassRequest : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string RivNumber { get; private set; } = default!;
    public Guid PurchaseRequestId { get; private set; }
    public DateOnly ReturnDeadline { get; private set; }
    public CanvassRequestStatus Status { get; private set; }
    public Guid? AwardedSupplierId { get; private set; }

    private readonly List<CanvassRequestLineItem> _lineItems = [];

    /// <summary>The PR line items this canvass covers (a subset of the PR's lines).</summary>
    public IReadOnlyList<CanvassRequestLineItem> LineItems => _lineItems.AsReadOnly();

    private readonly List<CanvassAwardSignatory> _awardSignatories = [];

    /// <summary>ROPC committee signatories frozen at award time for the Abstract of Canvass.</summary>
    public IReadOnlyList<CanvassAwardSignatory> AwardSignatories => _awardSignatories.AsReadOnly();

    /// <summary>The PR <c>ItemNo</c>s covered by this canvass — used for partition checks across canvasses.</summary>
    public IEnumerable<int> CoveredItemNos => _lineItems.Select(li => li.PrItemNo);

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

    public static CanvassRequest Create(
        string tenantId,
        string rivNumber,
        Guid purchaseRequestId,
        DateOnly returnDeadline,
        IEnumerable<CanvassRequestLineItemData> lineItems)
    {
        var canvass = new CanvassRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RivNumber = rivNumber,
            PurchaseRequestId = purchaseRequestId,
            ReturnDeadline = returnDeadline,
            Status = CanvassRequestStatus.Open,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        foreach (var li in lineItems)
        {
            canvass._lineItems.Add(CanvassRequestLineItem.Create(
                li.PrItemNo, li.Description, li.Unit, li.Quantity, li.EstimatedUnitCost, li.CatalogItemId, li.UacsObjectCode));
        }

        if (canvass._lineItems.Count == 0)
            throw new InvalidOperationException("A canvass request must cover at least one purchase request line item.");

        return canvass;
    }

    public void Award(Guid awardedSupplierId, IEnumerable<CanvassAwardSignatory>? signatories = null)
    {
        if (Status != CanvassRequestStatus.Open && Status != CanvassRequestStatus.Evaluated)
            throw new InvalidOperationException("Cannot award a canvass that is not Open or Evaluated.");

        Status = CanvassRequestStatus.Awarded;
        AwardedSupplierId = awardedSupplierId;

        // Freeze the committee that signed at award time so the Abstract of Canvass reprints faithfully.
        _awardSignatories.Clear();
        if (signatories is not null)
            _awardSignatories.AddRange(signatories);

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

