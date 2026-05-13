using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// PPE Issuance Report (PPEIR) — the transaction document for inter-office/department
/// transfer of PPE. Creating a PPEIR updates PPEItem.Status to Transferred.
/// The Property Transfer Report (PTR) is derived from PPEIR data — no separate entity.
/// </summary>
public sealed class PPEIssuanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Control number assigned by the Supply Officer.</summary>
    public string PPEIRNo { get; private set; } = default!;

    /// <summary>Date of issuance.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Employee/Supply Officer receiving the PPE.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid IssuedToEmployeeId { get; private set; }

    /// <summary>Office address of the receiving employee/Supply Officer.</summary>
    public string IssuedToOfficeAddress { get; private set; } = default!;

    /// <summary>
    /// Nature of the PPE issuance (Transfer to C.O./R.O./P.O., Donation, Dumping,
    /// Destruction, Sale, Others).
    /// </summary>
    public PPEIssuanceType IssuanceType { get; private set; }

    /// <summary>
    /// Issuing department's Division Chief who signed the PPEIR.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid IssuedByEmployeeId { get; private set; }

    /// <summary>
    /// Employee who received the PPE and signed.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReceivedByEmployeeId { get; private set; }

    /// <summary>Date the PPE was physically received by the recipient.</summary>
    public DateOnly? DateReceived { get; private set; }

    /// <summary>
    /// GSD-PSMD Division Chief who approved the issuance.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ApprovedByEmployeeId { get; private set; }

    /// <summary>Driver's printed name (for delivery vehicles).</summary>
    public string? DriverName { get; private set; }

    /// <summary>Bill of Lading number.</summary>
    public string? BillOfLadingNo { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static PPEIssuanceReport Create(
        string tenantId,
        string ppeirNo,
        DateOnly date,
        Guid issuedToEmployeeId,
        string issuedToOfficeAddress,
        PPEIssuanceType issuanceType,
        Guid issuedByEmployeeId,
        Guid receivedByEmployeeId,
        Guid approvedByEmployeeId,
        DateOnly? dateReceived = null,
        string? driverName = null,
        string? billOfLadingNo = null)
    {
        return new PPEIssuanceReport
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            PPEIRNo               = ppeirNo,
            Date                  = date,
            IssuedToEmployeeId    = issuedToEmployeeId,
            IssuedToOfficeAddress = issuedToOfficeAddress,
            IssuanceType          = issuanceType,
            IssuedByEmployeeId    = issuedByEmployeeId,
            ReceivedByEmployeeId  = receivedByEmployeeId,
            ApprovedByEmployeeId  = approvedByEmployeeId,
            DateReceived          = dateReceived,
            DriverName            = driverName,
            BillOfLadingNo        = billOfLadingNo,
            CreatedOnUtc          = DateTimeOffset.UtcNow,
        };
    }
}

