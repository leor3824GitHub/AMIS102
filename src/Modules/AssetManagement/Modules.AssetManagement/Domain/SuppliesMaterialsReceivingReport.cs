using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Supplies and Materials Receiving Report (SMRR) — NFA SOP GS-PD16 Exhibit 4.
/// Records inbound receipt of semi-expendable items from a supplier or transfer source.
/// Creating an SMRR automatically registers the received units as SemiExpendableProperty records.
/// </summary>
public sealed class SuppliesMaterialsReceivingReport : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Control number (e.g., SMRR-2024-00001).</summary>
    public string SMRRNo { get; private set; } = default!;

    /// <summary>Date the goods were received.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Name of the supplier or source agency.</summary>
    public string ReceivedFrom { get; private set; } = default!;

    public string? Address { get; private set; }

    /// <summary>Basis of receipt: Purchase, Transfer, Donation, or Others.</summary>
    public ReceiptType ReceiptType { get; private set; }

    /// <summary>Specify when ReceiptType = Others.</summary>
    public string? OtherReceiptType { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>Name of the supply officer who prepared/received the report.</summary>
    public string? ReceivedBy { get; private set; }

    /// <summary>Name of the approving officer.</summary>
    public string? NotedBy { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static SuppliesMaterialsReceivingReport Create(
        string smrrNo,
        DateOnly date,
        string receivedFrom,
        string? address,
        ReceiptType receiptType,
        string? otherReceiptType,
        string? fundCluster,
        string? receivedBy,
        string? notedBy)
    {
        return new SuppliesMaterialsReceivingReport
        {
            Id             = Guid.NewGuid(),
            SMRRNo         = smrrNo,
            Date           = date,
            ReceivedFrom   = receivedFrom,
            Address        = address,
            ReceiptType    = receiptType,
            OtherReceiptType = otherReceiptType,
            FundCluster    = fundCluster,
            ReceivedBy     = receivedBy,
            NotedBy        = notedBy,
            CreatedOnUtc   = DateTimeOffset.UtcNow,
        };
    }
}
