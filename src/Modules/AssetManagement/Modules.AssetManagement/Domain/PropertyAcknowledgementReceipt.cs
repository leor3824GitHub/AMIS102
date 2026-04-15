using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Property Acknowledgement Receipt (PAR) — in-office movement of accountability.
/// Assigns a PPE item to an accountable officer within the same office.
/// Creating a PAR updates PPEItem.Status to IssuedPAR.
/// </summary>
public sealed class PropertyAcknowledgementReceipt : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Control number assigned by the Supply Officer.</summary>
    public string PARNo { get; private set; } = default!;

    /// <summary>Date of issuance of the PAR.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Indicates whether the PAR is issued for a new purchase or a transfer.</summary>
    public PARType PARType { get; private set; }

    /// <summary>
    /// Employee issuing/releasing the PPE (supply officer or outgoing accountable officer).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReceivedFromEmployeeId { get; private set; }

    /// <summary>
    /// Employee receiving accountability for the PPE.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReceivedByEmployeeId { get; private set; }

    /// <summary>
    /// Approving official.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ApprovedByEmployeeId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static PropertyAcknowledgementReceipt Create(
        string parNo,
        DateOnly date,
        PARType parType,
        Guid receivedFromEmployeeId,
        Guid receivedByEmployeeId,
        Guid approvedByEmployeeId)
    {
        return new PropertyAcknowledgementReceipt
        {
            Id                     = Guid.NewGuid(),
            PARNo                  = parNo,
            Date                   = date,
            PARType                = parType,
            ReceivedFromEmployeeId = receivedFromEmployeeId,
            ReceivedByEmployeeId   = receivedByEmployeeId,
            ApprovedByEmployeeId   = approvedByEmployeeId,
            CreatedOnUtc           = DateTimeOffset.UtcNow,
        };
    }
}
