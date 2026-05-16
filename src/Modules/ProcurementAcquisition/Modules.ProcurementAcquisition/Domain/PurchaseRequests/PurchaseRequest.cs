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
    public string? ApprovedByName { get; private set; }
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

    public void Submit()
    {
        if (Status != PurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase requests can be submitted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase request must have at least one line item.");

        Status = PurchaseRequestStatus.Submitted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(string approvedByName)
    {
        if (Status != PurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted purchase requests can be approved.");

        Status = PurchaseRequestStatus.Approved;
        ApprovedByName = approvedByName;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != PurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted purchase requests can be rejected.");

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

