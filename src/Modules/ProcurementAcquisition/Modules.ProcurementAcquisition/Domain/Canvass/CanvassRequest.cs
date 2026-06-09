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

    // Per-line split award: the winning quotation/supplier for THIS line and the awarded unit price.
    // Null until the canvass is awarded. Different lines may resolve to different suppliers — that is
    // what lets one canvass fan out into one purchase order per winning supplier.
    public Guid? AwardedQuotationId { get; private set; }
    public Guid? AwardedSupplierId { get; private set; }
    public decimal? AwardedUnitPrice { get; private set; }

    private CanvassRequestLineItem() { }

    /// <summary>Records the winning quotation/supplier and awarded unit price for this line.</summary>
    public void AwardTo(Guid quotationId, Guid supplierId, decimal unitPrice)
    {
        AwardedQuotationId = quotationId;
        AwardedSupplierId = supplierId;
        AwardedUnitPrice = unitPrice;
    }

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

    /// <summary>
    /// Awards a <i>subset</i> of this canvass's covered lines to their winning quotations (partial award). Lines
    /// not in <paramref name="awardsByPrItemNo"/> are left untouched — they may be awarded later on this canvass or,
    /// in the multi-canvass-per-PR workflow, on a sibling canvass. Awards accumulate across calls; an already-awarded
    /// line cannot be re-awarded here. Each PR line may go to a different supplier, so the aggregate
    /// <see cref="AwardedSupplierId"/> reflects a single supplier only when every line awarded <i>so far</i> shares one
    /// (otherwise null — the per-line winners on <see cref="CanvassRequestLineItem"/> are authoritative). The
    /// cross-canvass "one canvass per awarded line" invariant is enforced by the command handler, which alone can see
    /// the PR's sibling canvasses.
    /// </summary>
    /// <param name="awardsByPrItemNo">The lines to award now: winning quotation, its supplier, and the awarded unit price.</param>
    /// <param name="signatories">ROPC committee frozen onto the canvass for the Abstract of Canvass.</param>
    public void AwardLines(
        IReadOnlyDictionary<int, (Guid QuotationId, Guid SupplierId, decimal UnitPrice)> awardsByPrItemNo,
        IEnumerable<CanvassAwardSignatory>? signatories = null)
    {
        if (Status == CanvassRequestStatus.Cancelled)
            throw new InvalidOperationException("Cannot award a cancelled canvass.");

        if (awardsByPrItemNo is null || awardsByPrItemNo.Count == 0)
            throw new InvalidOperationException("At least one line award is required.");

        // Reject awards that reference a line this canvass does not cover.
        var lineByPrItemNo = _lineItems.ToDictionary(li => li.PrItemNo);
        var extra = awardsByPrItemNo.Keys.Where(no => !lineByPrItemNo.ContainsKey(no)).ToList();
        if (extra.Count > 0)
            throw new InvalidOperationException(
                $"Award(s) reference line(s) not covered by this canvass: {string.Join(", ", extra)}.");

        // Partial awards accumulate; a line already awarded on this canvass is not re-awarded.
        var alreadyAwarded = awardsByPrItemNo.Keys
            .Where(no => lineByPrItemNo[no].AwardedQuotationId is not null)
            .ToList();
        if (alreadyAwarded.Count > 0)
            throw new InvalidOperationException(
                $"Line(s) already awarded on this canvass: {string.Join(", ", alreadyAwarded)}.");

        foreach (var (prItemNo, award) in awardsByPrItemNo)
            lineByPrItemNo[prItemNo].AwardTo(award.QuotationId, award.SupplierId, award.UnitPrice);

        // Recompute award flags from ALL lines awarded so far (this call plus any earlier partial awards).
        var winningQuotationIds = _lineItems
            .Where(li => li.AwardedQuotationId is not null)
            .Select(li => li.AwardedQuotationId!.Value)
            .ToHashSet();
        foreach (var q in Quotations)
        {
            if (winningQuotationIds.Contains(q.Id))
                q.MarkAwarded();
            else
                q.ClearAwarded();
        }

        // Aggregate convenience: a single supplier only when every awarded line so far shares one; otherwise null.
        var distinctSuppliers = _lineItems
            .Where(li => li.AwardedSupplierId is not null)
            .Select(li => li.AwardedSupplierId!.Value)
            .Distinct()
            .ToList();
        AwardedSupplierId = distinctSuppliers.Count == 1 ? distinctSuppliers[0] : null;

        Status = CanvassRequestStatus.Awarded;

        // Freeze (or refresh) the committee that signed at award time so the Abstract of Canvass reprints faithfully.
        if (signatories is not null)
        {
            _awardSignatories.Clear();
            _awardSignatories.AddRange(signatories);
        }

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

