using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Receipt for Returned Property (RRP) — in-office return of PPE accountability.
/// Prepared by the Supply Officer who receives the returned/surrendered PPE.
/// Separate control number series for Serviceable (reissue) and Junked (disposal) items.
/// Creating an RRP updates PPEItem.Status: Serviceable → OnHand, Junked → Disposed.
/// </summary>
public sealed class ReceiptForReturnedPPE : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// Control number assigned by the Supply Officer.
    /// Separate sequences maintained for Serviceable and Junked categories.
    /// </summary>
    public string RRPNo { get; private set; } = default!;

    /// <summary>Date of the return.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Category of the returned PPE. Determines downstream action:
    /// Serviceable → back to supply stock; Junked → forwarded to Accounting for disposal.
    /// </summary>
    public PPEReturnCategory ReturnCategory { get; private set; }

    /// <summary>
    /// Employee surrendering the PPE (must bring Certification from Property Inspector).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReturnedByEmployeeId { get; private set; }

    /// <summary>
    /// GSD-PSMD Division Chief / RM / PM who approved the return.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ApprovedByEmployeeId { get; private set; }

    /// <summary>
    /// Supply Officer who signed the RRP.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid SignedByEmployeeId { get; private set; }

    /// <summary>
    /// Indicates whether the Property Inspector has issued a Certification
    /// for the returned PPE (required before RRP can be processed).
    /// </summary>
    public bool PropertyInspectorCertified { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static ReceiptForReturnedPPE Create(
        string tenantId,
        string rrpNo,
        DateOnly date,
        PPEReturnCategory returnCategory,
        Guid returnedByEmployeeId,
        Guid approvedByEmployeeId,
        Guid signedByEmployeeId,
        bool propertyInspectorCertified)
    {
        return new ReceiptForReturnedPPE
        {
            Id                        = Guid.NewGuid(),
            TenantId                  = tenantId,
            RRPNo                     = rrpNo,
            Date                      = date,
            ReturnCategory            = returnCategory,
            ReturnedByEmployeeId      = returnedByEmployeeId,
            ApprovedByEmployeeId      = approvedByEmployeeId,
            SignedByEmployeeId        = signedByEmployeeId,
            PropertyInspectorCertified = propertyInspectorCertified,
            CreatedOnUtc              = DateTimeOffset.UtcNow,
        };
    }
}
