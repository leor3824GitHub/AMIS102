using AMIS.Framework.Core.Domain;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;

namespace AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests;

public sealed class PurchaseRequestLineItem
{
    public int ItemNo { get; private set; }
    public decimal Quantity { get; private set; }
    public string UnitOfIssue { get; private set; } = default!;
    public string ItemDescription { get; private set; } = default!;
    public decimal EstimatedUnitCost { get; private set; }
    public decimal EstimatedTotalCost => Quantity * EstimatedUnitCost;

    /// <summary>
    /// UACS Object Code assigned by the Accountant during the "Funds Available" certification step.
    /// Null until the Accountant certifies the PR; carries forward to PO/IAR/PPERR thereafter.
    /// </summary>
    public string? UacsObjectCode { get; private set; }

    private PurchaseRequestLineItem() { }

    public static PurchaseRequestLineItem Create(
        int itemNo,
        decimal quantity,
        string unitOfIssue,
        string itemDescription,
        decimal estimatedUnitCost)
    {
        return new PurchaseRequestLineItem
        {
            ItemNo = itemNo,
            Quantity = quantity,
            UnitOfIssue = unitOfIssue,
            ItemDescription = itemDescription,
            EstimatedUnitCost = estimatedUnitCost
        };
    }

    public void Update(decimal quantity, string unitOfIssue, string itemDescription, decimal estimatedUnitCost)
    {
        Quantity = quantity;
        UnitOfIssue = unitOfIssue;
        ItemDescription = itemDescription;
        EstimatedUnitCost = estimatedUnitCost;
    }

    internal void AssignUacs(string uacsObjectCode)
    {
        if (string.IsNullOrWhiteSpace(uacsObjectCode))
            throw new InvalidOperationException($"Item {ItemNo}: UACS Object Code is required.");
        UacsObjectCode = uacsObjectCode.Trim();
    }
}

public sealed class PurchaseRequest : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string PrNumber { get; private set; } = default!;
    public DateOnly PrDate { get; private set; }
    public string? SaiNumber { get; private set; }
    public DateOnly? SaiDate { get; private set; }
    public string? AlobsNumber { get; private set; }
    public DateOnly? AlobsDate { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string? ResponsibilityCenterCode { get; private set; }
    public string Purpose { get; private set; } = default!;
    public PrType PrType { get; private set; }
    public string? Justification { get; private set; }
    public PurchaseRequestStatus Status { get; private set; }
    public string RequestedByName { get; private set; } = default!;

    // "Funds Available" — Accountant signs and assigns UACS codes
    public Guid? FundsAvailableCertifiedById { get; private set; }
    public string? FundsAvailableCertifiedByName { get; private set; }
    public DateTimeOffset? FundsAvailableCertifiedOnUtc { get; private set; }

    // "Approved by" — HoPE
    public string? ApprovedByName { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTimeOffset? ApprovedOnUtc { get; private set; }

    // Return for revision — set by either approver; cleared when re-submitted
    public string? ReturnedReason { get; private set; }
    public Guid? ReturnedById { get; private set; }
    public string? ReturnedByName { get; private set; }
    public DateTimeOffset? ReturnedOnUtc { get; private set; }

    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] Version { get; set; } = [];

    private readonly List<PurchaseRequestLineItem> _lineItems = [];
    public IReadOnlyList<PurchaseRequestLineItem> LineItems => _lineItems.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private PurchaseRequest() { }

    public static PurchaseRequest Create(
        string tenantId,
        string prNumber,
        Guid departmentId,
        string? responsibilityCenterCode,
        string purpose,
        PrType prType,
        string? justification,
        string requestedByName,
        string? saiNumber,
        DateOnly? saiDate,
        string? alobsNumber,
        DateOnly? alobsDate,
        IEnumerable<(decimal Quantity, string UnitOfIssue, string ItemDescription, decimal EstimatedUnitCost)> lineItems)
    {
        var pr = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PrNumber = prNumber,
            PrDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DepartmentId = departmentId,
            ResponsibilityCenterCode = responsibilityCenterCode,
            Purpose = purpose,
            PrType = prType,
            Justification = justification,
            RequestedByName = requestedByName,
            SaiNumber = saiNumber,
            SaiDate = saiDate,
            AlobsNumber = alobsNumber,
            AlobsDate = alobsDate,
            Status = PurchaseRequestStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var (qty, unit, desc, cost) in lineItems)
        {
            pr._lineItems.Add(PurchaseRequestLineItem.Create(itemNo++, qty, unit, desc, cost));
        }

        return pr;
    }

    public void Update(
        Guid departmentId,
        string? responsibilityCenterCode,
        string purpose,
        PrType prType,
        string? justification,
        string requestedByName,
        string? saiNumber,
        DateOnly? saiDate,
        string? alobsNumber,
        DateOnly? alobsDate,
        IEnumerable<(decimal Quantity, string UnitOfIssue, string ItemDescription, decimal EstimatedUnitCost)> lineItems)
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase requests can be updated.");

        DepartmentId = departmentId;
        ResponsibilityCenterCode = responsibilityCenterCode;
        Purpose = purpose;
        PrType = prType;
        Justification = justification;
        RequestedByName = requestedByName;
        SaiNumber = saiNumber;
        SaiDate = saiDate;
        AlobsNumber = alobsNumber;
        AlobsDate = alobsDate;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var (qty, unit, desc, cost) in lineItems)
        {
            _lineItems.Add(PurchaseRequestLineItem.Create(itemNo++, qty, unit, desc, cost));
        }
    }

    /// <summary>
    /// Requester submits the PR. Moves Draft → PendingFundsAvailable (awaiting Accountant).
    /// </summary>
    public void Submit()
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase requests can be submitted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase request must have at least one line item.");

        Status = PurchaseRequestStatus.PendingFundsAvailable;
        // Clear any prior return metadata on re-submission
        ReturnedReason = null;
        ReturnedById = null;
        ReturnedByName = null;
        ReturnedOnUtc = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Accountant signs "Funds Available", assigns a UACS Object Code per line item, and optionally
    /// captures ALOBS reference. Moves PendingFundsAvailable → PendingApproval (awaiting HoPE).
    /// </summary>
    public void CertifyFundsAvailable(
        Guid certifiedById,
        string certifiedByName,
        IReadOnlyDictionary<int, string> uacsByItemNo,
        string? alobsNumber,
        DateOnly? alobsDate)
    {
        if (Status != PurchaseRequestStatus.PendingFundsAvailable)
            throw new InvalidOperationException("Funds Available can only be certified on PRs awaiting Accountant review.");
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

        if (!string.IsNullOrWhiteSpace(alobsNumber))
        {
            AlobsNumber = alobsNumber;
            AlobsDate = alobsDate;
        }

        FundsAvailableCertifiedById = certifiedById;
        FundsAvailableCertifiedByName = certifiedByName;
        FundsAvailableCertifiedOnUtc = DateTimeOffset.UtcNow;

        Status = PurchaseRequestStatus.PendingApproval;
        LastModifiedOnUtc = FundsAvailableCertifiedOnUtc;
    }

    /// <summary>
    /// HoPE final approval. Allowed from PendingApproval (new two-step flow) or from
    /// legacy <see cref="PurchaseRequestStatus.Submitted"/> (rows created before the workflow change).
    /// </summary>
    public void Approve(string approvedByName, Guid? approvedById = null)
    {
        if (Status != PurchaseRequestStatus.PendingApproval && Status != PurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only PRs awaiting HoPE approval can be approved.");
        if (string.IsNullOrWhiteSpace(approvedByName))
            throw new InvalidOperationException("Approver name is required.");

        Status = PurchaseRequestStatus.Approved;
        ApprovedByName = approvedByName;
        ApprovedById = approvedById;
        ApprovedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ApprovedOnUtc;
    }

    /// <summary>
    /// Either approver returns the PR for revision. Reverts to Draft with a reason; requester re-edits and resubmits.
    /// </summary>
    public void ReturnForRevision(Guid returnedById, string returnedByName, string reason)
    {
        if (Status != PurchaseRequestStatus.PendingFundsAvailable && Status != PurchaseRequestStatus.PendingApproval)
            throw new InvalidOperationException("Only PRs awaiting approval can be returned for revision.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A reason is required when returning a PR for revision.");
        if (string.IsNullOrWhiteSpace(returnedByName))
            throw new InvalidOperationException("Returner name is required.");

        Status = PurchaseRequestStatus.Draft;
        ReturnedReason = reason;
        ReturnedById = returnedById;
        ReturnedByName = returnedByName;
        ReturnedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = ReturnedOnUtc;
    }

    public void Reject(string reason)
    {
        if (Status != PurchaseRequestStatus.PendingApproval && Status != PurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only PRs awaiting HoPE approval can be rejected.");

        Status = PurchaseRequestStatus.Rejected;
        RejectionReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == PurchaseRequestStatus.Approved || Status == PurchaseRequestStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a purchase request with status '{Status}'.");

        Status = PurchaseRequestStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
