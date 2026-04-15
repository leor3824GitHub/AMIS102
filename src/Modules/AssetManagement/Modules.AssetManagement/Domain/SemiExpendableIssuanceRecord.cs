using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Semi-expendable Materials Issuance Record (SMIR) — the transaction document that
/// records the outgoing transfer of semi-expendable property FROM the current office
/// (tenant) TO another office (tenant) or external recipient.
///
/// Equivalent to the PPEIR (PPE Issuance Report) for semi-expendable property.
/// The ITR (Inventory Transfer Report) is a summary report generated from SMIR data.
///
/// Business rules:
///   - All transferred properties must be OnHand or Returned (no active ICS).
///   - The supply officer cannot issue a SMIR if any property still has an active ICS —
///     the employee must first return the property via RRSP.
///   - After processing, properties are marked Transferred and removed from the active
///     inventory of the sending office; the audit trail is preserved.
///   - The receiving office records its own SMRR when items physically arrive.
/// </summary>
public sealed class SemiExpendableIssuanceRecord : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Control number assigned by the Supply Division.
    /// Recommended format: SMIR-YYYY-NNNN.
    /// </summary>
    public string SMIRNo { get; private set; } = default!;

    public DateOnly Date { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>Nature of the issuance (Transfer, Donation, Disposal, Sale, Others).</summary>
    public SMIRIssuanceType IssuanceType { get; private set; }

    /// <summary>
    /// Tenant ID of the receiving office.
    /// Each office is a separate tenant; this links the transfer to its destination.
    /// Null when IssuanceType is Donation, Disposal, or Sale (no receiving tenant).
    /// </summary>
    public string? TransferredToTenantId { get; private set; }

    /// <summary>
    /// Name of the receiving accountable officer or head of office (attn: supply officer).
    /// Free-text since the receiving office is a different tenant.
    /// </summary>
    public string? TransferredToOfficerName { get; private set; }

    /// <summary>
    /// The supply officer of the sending office who authorized and released the items.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? IssuedByEmployeeId { get; private set; }

    public string? Remarks { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static SemiExpendableIssuanceRecord Create(
        string smirNo,
        DateOnly date,
        string? fundCluster,
        SMIRIssuanceType issuanceType,
        string? transferredToTenantId,
        string? transferredToOfficerName,
        Guid? issuedByEmployeeId,
        string? remarks)
    {
        return new SemiExpendableIssuanceRecord
        {
            Id                      = Guid.NewGuid(),
            SMIRNo                  = smirNo,
            Date                    = date,
            FundCluster             = fundCluster,
            IssuanceType            = issuanceType,
            TransferredToTenantId   = transferredToTenantId,
            TransferredToOfficerName = transferredToOfficerName,
            IssuedByEmployeeId      = issuedByEmployeeId,
            Remarks                 = remarks,
            CreatedOnUtc            = DateTimeOffset.UtcNow,
        };
    }
}
