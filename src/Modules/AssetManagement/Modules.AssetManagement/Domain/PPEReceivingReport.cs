using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// PPE Receiving Report (PPERR) — documents receipt of PPE from a supplier, donor, or transfer.
/// Creates PPEItem records upon issuance.
/// </summary>
public sealed class PPEReceivingReport : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Control number assigned by the Supply Officer.</summary>
    public string PPERRNo { get; private set; } = default!;

    /// <summary>Date of the PPE receipt.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Name of the supplier, issuing supply officer, or donor.</summary>
    public string ReceivedFrom { get; private set; } = default!;

    /// <summary>Address of the supplier/issuing supply officer/donor.</summary>
    public string Address { get; private set; } = default!;

    /// <summary>Nature of the PPE receipt (Purchase/Transfer/Donation/Others).</summary>
    public PPEReceiptNature ReceiptNature { get; private set; }

    /// <summary>
    /// Employee who received and signed for the PPE (receiving supply officer).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReceivedByEmployeeId { get; private set; }

    /// <summary>
    /// GSD-PSMD or FDC-SSD Division Chief who noted the receipt.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid NotedByEmployeeId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static PPEReceivingReport Create(
        string pperrNo,
        DateOnly date,
        string receivedFrom,
        string address,
        PPEReceiptNature receiptNature,
        Guid receivedByEmployeeId,
        Guid notedByEmployeeId)
    {
        return new PPEReceivingReport
        {
            Id                   = Guid.NewGuid(),
            PPERRNo              = pperrNo,
            Date                 = date,
            ReceivedFrom         = receivedFrom,
            Address              = address,
            ReceiptNature        = receiptNature,
            ReceivedByEmployeeId = receivedByEmployeeId,
            NotedByEmployeeId    = notedByEmployeeId,
            CreatedOnUtc         = DateTimeOffset.UtcNow,
        };
    }
}
