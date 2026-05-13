using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Receipt for Returned Semi-Expendable Property (RRSP) — COA Circular 2022-004 Annex A.4.
/// Documents when an end-user returns semi-expendable property to the supply officer.
/// Creating an RRSP cancels the referenced ICS and sets all returned property units
/// back to Returned status (clearing the CurrentCustodianId).
/// </summary>
public sealed class ReceiptForReturnedProperty : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// Control number. Suggested format: RRSP-YYYY-MM-NNNN.
    /// </summary>
    public string RRSPNo { get; private set; } = default!;

    public DateOnly Date { get; private set; }

    /// <summary>
    /// The ICS that this RRSP cancels. Must be Active at the time of creation.
    /// </summary>
    public Guid ICSId { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>
    /// Supply officer receiving the returned property.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? ReceivedByEmployeeId { get; private set; }

    /// <summary>
    /// End-user returning the property.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid ReturnedByEmployeeId { get; private set; }

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

    public static ReceiptForReturnedProperty Create(
        string tenantId,
        string rrspNo,
        DateOnly date,
        Guid icsId,
        string? fundCluster,
        Guid? receivedByEmployeeId,
        Guid returnedByEmployeeId,
        string? remarks)
    {
        return new ReceiptForReturnedProperty
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            RRSPNo                = rrspNo,
            Date                  = date,
            ICSId                 = icsId,
            FundCluster           = fundCluster,
            ReceivedByEmployeeId  = receivedByEmployeeId,
            ReturnedByEmployeeId  = returnedByEmployeeId,
            Remarks               = remarks,
            CreatedOnUtc          = DateTimeOffset.UtcNow,
        };
    }
}

