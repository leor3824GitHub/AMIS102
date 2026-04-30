using FSH.Framework.Core.Domain;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;

namespace FSH.Modules.AssetProcurement.Domain.AssetPurchaseRequests;

public sealed class AssetPurchaseRequestLineItem
{
    public int ItemNo { get; private set; }
    public string ItemDescription { get; private set; } = default!;
    public string? TechnicalSpecifications { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? PropertyClassHint { get; private set; }
    public string Unit { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public decimal EstimatedTotalCost => Quantity * EstimatedUnitCost;

    private AssetPurchaseRequestLineItem() { }

    public static AssetPurchaseRequestLineItem Create(
        int itemNo,
        string itemDescription,
        string? technicalSpecifications,
        string? brand,
        string? model,
        string? propertyClassHint,
        string unit,
        decimal quantity,
        decimal estimatedUnitCost) =>
        new()
        {
            ItemNo = itemNo,
            ItemDescription = itemDescription,
            TechnicalSpecifications = technicalSpecifications,
            Brand = brand,
            Model = model,
            PropertyClassHint = propertyClassHint,
            Unit = unit,
            Quantity = quantity,
            EstimatedUnitCost = estimatedUnitCost
        };

    public void Update(
        string itemDescription,
        string? technicalSpecifications,
        string? brand,
        string? model,
        string? propertyClassHint,
        string unit,
        decimal quantity,
        decimal estimatedUnitCost)
    {
        ItemDescription = itemDescription;
        TechnicalSpecifications = technicalSpecifications;
        Brand = brand;
        Model = model;
        PropertyClassHint = propertyClassHint;
        Unit = unit;
        Quantity = quantity;
        EstimatedUnitCost = estimatedUnitCost;
    }
}

public sealed class AssetPurchaseRequest : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string PrNumber { get; private set; } = default!;
    public DateOnly PrDate { get; private set; }
    public string? SaiNumber { get; private set; }
    public DateOnly? SaiDate { get; private set; }
    public string? AlobsNumber { get; private set; }
    public DateOnly? AlobsDate { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string? Section { get; private set; }
    public string Purpose { get; private set; } = default!;
    public AssetPrType PrType { get; private set; }
    public string? Justification { get; private set; }
    public AssetPurchaseRequestStatus Status { get; private set; }
    public Guid RequestedById { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] Version { get; set; } = [];

    private readonly List<AssetPurchaseRequestLineItem> _lineItems = [];
    public IReadOnlyList<AssetPurchaseRequestLineItem> LineItems => _lineItems.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private AssetPurchaseRequest() { }

    public static AssetPurchaseRequest Create(
        string tenantId,
        string prNumber,
        Guid departmentId,
        string? section,
        string purpose,
        AssetPrType prType,
        string? justification,
        Guid requestedById,
        string? saiNumber,
        DateOnly? saiDate,
        string? alobsNumber,
        DateOnly? alobsDate,
        IEnumerable<AssetPurchaseRequestLineItemRequest> lineItems)
    {
        var pr = new AssetPurchaseRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PrNumber = prNumber,
            PrDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DepartmentId = departmentId,
            Section = section,
            Purpose = purpose,
            PrType = prType,
            Justification = justification,
            RequestedById = requestedById,
            SaiNumber = saiNumber,
            SaiDate = saiDate,
            AlobsNumber = alobsNumber,
            AlobsDate = alobsDate,
            Status = AssetPurchaseRequestStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        var itemNo = 1;
        foreach (var li in lineItems)
            pr._lineItems.Add(AssetPurchaseRequestLineItem.Create(
                itemNo++, li.ItemDescription, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint, li.Unit,
                li.Quantity, li.EstimatedUnitCost));

        return pr;
    }

    public void Update(
        Guid departmentId,
        string? section,
        string purpose,
        AssetPrType prType,
        string? justification,
        Guid requestedById,
        string? saiNumber,
        DateOnly? saiDate,
        string? alobsNumber,
        DateOnly? alobsDate,
        IEnumerable<AssetPurchaseRequestLineItemRequest> lineItems)
    {
        if (Status != AssetPurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase requests can be updated.");

        DepartmentId = departmentId;
        Section = section;
        Purpose = purpose;
        PrType = prType;
        Justification = justification;
        RequestedById = requestedById;
        SaiNumber = saiNumber;
        SaiDate = saiDate;
        AlobsNumber = alobsNumber;
        AlobsDate = alobsDate;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        _lineItems.Clear();
        var itemNo = 1;
        foreach (var li in lineItems)
            _lineItems.Add(AssetPurchaseRequestLineItem.Create(
                itemNo++, li.ItemDescription, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint, li.Unit,
                li.Quantity, li.EstimatedUnitCost));
    }

    public void Submit()
    {
        if (Status != AssetPurchaseRequestStatus.Draft)
            throw new InvalidOperationException("Only Draft purchase requests can be submitted.");
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Purchase request must have at least one line item.");

        Status = AssetPurchaseRequestStatus.Submitted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid approvedById)
    {
        if (Status != AssetPurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted purchase requests can be approved.");

        Status = AssetPurchaseRequestStatus.Approved;
        ApprovedById = approvedById;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != AssetPurchaseRequestStatus.Submitted)
            throw new InvalidOperationException("Only Submitted purchase requests can be rejected.");

        Status = AssetPurchaseRequestStatus.Rejected;
        RejectionReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status is AssetPurchaseRequestStatus.Approved or AssetPurchaseRequestStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a purchase request with status '{Status}'.");

        Status = AssetPurchaseRequestStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
