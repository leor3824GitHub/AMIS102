using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// A Physical Count Session — the top-level document that tracks a physical inventory
/// walk-through for a specific office/station on a given date.
///
/// Per COA Circular 2020-006:
///   - The Inventory Count Form (ICF) is the working document produced during the count.
///   - The Report on the Physical Count of PPE (RPCPPE) is generated when the session is submitted.
///   - A session may cover PPE only, Semi-Expendable only, or both tracks.
/// </summary>
public sealed class PhysicalCountSession : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// Control number for this count session (e.g., PCS-2026-00001).
    /// Assigned by the Supply Officer.
    /// </summary>
    public string SessionNo { get; private set; } = default!;

    /// <summary>The date the physical count was conducted.</summary>
    public DateOnly CountDate { get; private set; }

    /// <summary>
    /// The office, division, or station where the physical count was conducted.
    /// Free-text (e.g., "IT Division – Head Office").
    /// </summary>
    public string StationOffice { get; private set; } = default!;

    /// <summary>Asset tracks covered by this session.</summary>
    public PhysicalCountScope Scope { get; private set; }

    /// <summary>Current status of the session (Open or Submitted).</summary>
    public PhysicalCountStatus Status { get; private set; } = PhysicalCountStatus.Open;

    // ── Signatories ───────────────────────────────────────────────────────────

    /// <summary>
    /// Employee who conducted the count / prepared the ICF.
    /// References MasterData.EmployeeProfile — plain FK.
    /// </summary>
    public Guid PreparedByEmployeeId { get; private set; }

    /// <summary>
    /// Certifying officer — confirms that the count was actually conducted.
    /// References MasterData.EmployeeProfile — plain FK.
    /// </summary>
    public Guid CertifiedByEmployeeId { get; private set; }

    /// <summary>
    /// Approving authority (e.g., GSD-PSMD Division Chief).
    /// References MasterData.EmployeeProfile — plain FK.
    /// </summary>
    public Guid ApprovedByEmployeeId { get; private set; }

    // ── Submission metadata ───────────────────────────────────────────────────

    /// <summary>UTC timestamp when the session was submitted and locked.</summary>
    public DateTimeOffset? SubmittedOnUtc { get; private set; }

    // ── IAuditableEntity ─────────────────────────────────────────────────────

    public byte[] Version { get; set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    // ─────────────────────────────────────────────────────────────────────────

    public static PhysicalCountSession Create(
        string tenantId,
        string sessionNo,
        DateOnly countDate,
        string stationOffice,
        PhysicalCountScope scope,
        Guid preparedByEmployeeId,
        Guid certifiedByEmployeeId,
        Guid approvedByEmployeeId)
    {
        return new PhysicalCountSession
        {
            Id                     = Guid.NewGuid(),
            TenantId               = tenantId,
            SessionNo              = sessionNo,
            CountDate              = countDate,
            StationOffice          = stationOffice,
            Scope                  = scope,
            Status                 = PhysicalCountStatus.Open,
            PreparedByEmployeeId   = preparedByEmployeeId,
            CertifiedByEmployeeId  = certifiedByEmployeeId,
            ApprovedByEmployeeId   = approvedByEmployeeId,
            CreatedOnUtc           = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Locks the session and records the submission timestamp.
    /// Called when the inventory team finalises the count and generates the RPCPPE/ICF.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the session is already submitted.</exception>
    public void Submit()
    {
        if (Status == PhysicalCountStatus.Submitted)
            throw new InvalidOperationException("Physical count session is already submitted.");

        Status          = PhysicalCountStatus.Submitted;
        SubmittedOnUtc  = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

