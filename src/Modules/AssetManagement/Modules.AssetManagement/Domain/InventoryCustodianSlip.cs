using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Inventory Custodian Slip (ICS) — COA Circular 2022-004 Annex A.3.
/// Used to issue semi-expendable property (below capitalization threshold) to an end-user.
/// Creating an ICS sets the referenced SemiExpendableProperty units to Issued status
/// and assigns them to the receiving employee (CurrentCustodianId).
/// </summary>
public sealed class InventoryCustodianSlip : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// Control number. Format depends on category:
    ///   Low-valued:  SPLV-YYYY-MM-NNNN
    ///   High-valued: SPHV-YYYY-MM-NNNN
    /// </summary>
    public string ICSNo { get; private set; } = default!;

    public DateOnly Date { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>
    /// Employee who issued the property (supply officer).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? IssuedFromEmployeeId { get; private set; }

    /// <summary>
    /// Employee receiving the property (end-user / accountable officer).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReceivedByEmployeeId { get; private set; }

    /// <summary>
    /// Asset type for all items on this ICS. Always SE for ICS documents.
    /// Kept on the header for quick filtering; all items must be SE.
    /// </summary>
    public AssetType AssetType { get; private set; }

    /// <summary>
    /// Current lifecycle status of this ICS.
    /// </summary>
    public ICSStatus Status { get; private set; } = ICSStatus.Active;

    /// <summary>
    /// Date this ICS expires and must be renewed.
    /// Set to Date.AddYears(3) at creation per COA Circular 2022-004 Section 4.11.
    /// Null for ICS records created before this field was introduced (legacy).
    /// </summary>
    public DateOnly? ExpiresOn { get; private set; }

    // ── Lifecycle link fields ──────────────────────────────────────────────────

    /// <summary>
    /// If this ICS is a renewal, the ID of the previous ICS it replaced.
    /// Populated by RenewICSCommand.
    /// </summary>
    public Guid? RenewedFromICSId { get; private set; }

    /// <summary>
    /// If this ICS has been renewed, the ID of the ICS that replaced it.
    /// Populated when Status is set to Renewed.
    /// </summary>
    public Guid? RenewedByICSId { get; private set; }

    /// <summary>
    /// If this ICS was cancelled by a return, the ID of the RRSP that closed it.
    /// Populated by CreateRRSPCommand.
    /// </summary>
    public Guid? CancelledByRRSPId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static InventoryCustodianSlip Create(
        string tenantId,
        string icsNo,
        DateOnly date,
        string? fundCluster,
        Guid? issuedFromEmployeeId,
        Guid receivedByEmployeeId,
        Guid? renewedFromICSId = null)
    {
        return new InventoryCustodianSlip
        {
            Id                   = Guid.NewGuid(),
            TenantId             = tenantId,
            ICSNo                = icsNo,
            Date                 = date,
            AssetType            = AssetType.SE,
            FundCluster          = fundCluster,
            IssuedFromEmployeeId = issuedFromEmployeeId,
            ReceivedByEmployeeId = receivedByEmployeeId,
            Status               = ICSStatus.Active,
            ExpiresOn            = date.AddYears(3),
            RenewedFromICSId     = renewedFromICSId,
            CreatedOnUtc         = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Marks this ICS as renewed. Called when a new ICS is issued to replace this one.
    /// </summary>
    public void MarkRenewed(Guid newICSId)
    {
        Status = ICSStatus.Renewed;
        RenewedByICSId = newICSId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this ICS as cancelled due to all items being returned (RRSP).
    /// </summary>
    public void CancelByReturn(Guid rrspId)
    {
        Status = ICSStatus.CancelledByReturn;
        CancelledByRRSPId = rrspId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this ICS as expired. Called by the Hangfire background job when
    /// low-valued items reach end of estimated useful life.
    /// </summary>
    public void MarkExpired()
    {
        Status = ICSStatus.Expired;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
