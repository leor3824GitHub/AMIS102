using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Inventory Custodian Slip (ICS) — COA Circular 2022-004 Annex A.3.
/// Used to issue semi-expendable property (below capitalization threshold) to an end-user.
/// Creating an ICS sets the referenced SemiExpendableProperty units to Issued status
/// and assigns them to the receiving employee (CurrentCustodianId).
/// </summary>
public sealed class InventoryCustodianSlip : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>Control number (e.g., ICS-2024-00001).</summary>
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
        string icsNo,
        DateOnly date,
        string? fundCluster,
        Guid? issuedFromEmployeeId,
        Guid receivedByEmployeeId)
    {
        return new InventoryCustodianSlip
        {
            Id                  = Guid.NewGuid(),
            ICSNo               = icsNo,
            Date                = date,
            FundCluster         = fundCluster,
            IssuedFromEmployeeId = issuedFromEmployeeId,
            ReceivedByEmployeeId = receivedByEmployeeId,
            CreatedOnUtc        = DateTimeOffset.UtcNow,
        };
    }
}
