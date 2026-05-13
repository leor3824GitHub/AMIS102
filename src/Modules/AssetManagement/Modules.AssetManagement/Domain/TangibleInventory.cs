using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Unified Tangible Inventory receiving document — replaces both the
/// Supplies and Materials Receiving Report (SMRR) and PPE Receiving Report (PPERR).
/// A single report can contain a mix of SE and PPE line items; the classification
/// is determined per-item by comparing UnitCost against the active capitalization threshold.
/// </summary>
public sealed class TangibleInventory : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Control number (e.g., TI-2024-00001).</summary>
    public string ReportNo { get; private set; } = default!;

    /// <summary>Date the goods were received.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Name of the supplier, donor, or source agency.</summary>
    public string ReceivedFrom { get; private set; } = default!;

    /// <summary>Address of the supplier or source.</summary>
    public string? Address { get; private set; }

    /// <summary>Basis of receipt: Purchase, Transfer, Donation, or Others.</summary>
    public ReceiptType ReceiptType { get; private set; }

    /// <summary>Specify when ReceiptType = Others.</summary>
    public string? OtherReceiptType { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>
    /// Employee who received and signed for the goods.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? ReceivedByEmployeeId { get; private set; }

    /// <summary>
    /// Employee ID of the approving/noting officer.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? NotedByEmployeeId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public void Update(
        string reportNo,
        DateOnly date,
        string receivedFrom,
        string? address,
        ReceiptType receiptType,
        string? otherReceiptType,
        string? fundCluster,
        Guid? receivedByEmployeeId,
        Guid? notedByEmployeeId)
    {
        ReportNo             = reportNo;
        Date                 = date;
        ReceivedFrom         = receivedFrom;
        Address              = address;
        ReceiptType          = receiptType;
        OtherReceiptType     = otherReceiptType;
        FundCluster          = fundCluster;
        ReceivedByEmployeeId = receivedByEmployeeId;
        NotedByEmployeeId    = notedByEmployeeId;
    }

    public static TangibleInventory Create(
        string tenantId,
        string reportNo,
        DateOnly date,
        string receivedFrom,
        string? address,
        ReceiptType receiptType,
        string? otherReceiptType,
        string? fundCluster,
        Guid? receivedByEmployeeId,
        Guid? notedByEmployeeId)
    {
        return new TangibleInventory
        {
            Id                   = Guid.NewGuid(),
            TenantId             = tenantId,
            ReportNo             = reportNo,
            Date                 = date,
            ReceivedFrom         = receivedFrom,
            Address              = address,
            ReceiptType          = receiptType,
            OtherReceiptType     = otherReceiptType,
            FundCluster          = fundCluster,
            ReceivedByEmployeeId = receivedByEmployeeId,
            NotedByEmployeeId    = notedByEmployeeId,
            CreatedOnUtc         = DateTimeOffset.UtcNow,
        };
    }
}

