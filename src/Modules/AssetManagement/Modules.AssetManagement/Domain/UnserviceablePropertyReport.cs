using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Inspection and Inventory Report of Unserviceable Semi-Expendable Property (IIRUSP) —
/// COA Circular 2022-004 Annex A.6.
///
/// Documents property found to be beyond economical repair and authorises its disposal.
/// Creating this report sets the referenced SemiExpendableProperty units to
/// PropertyStatus.Disposed and clears the CurrentCustodianId.
///
/// Eligible property statuses: OnHand, Returned, LostStolenDamaged.
/// If the property is Issued, the employee must first return it via RRSP before
/// it can be included in this report.
/// </summary>
public sealed class UnserviceablePropertyReport : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Control number. Suggested format: IUR-YYYY-MM-NNNN
    /// (e.g. IUR-2024-01-0001).
    /// </summary>
    public string ReportNo { get; private set; } = default!;

    /// <summary>Date the inspection / inventory report was prepared.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Recommended method of disposal for all items on this report.
    /// </summary>
    public DisposalMethod DisposalMethod { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>
    /// Employee who inspected and certified the items as unserviceable.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? InspectedByEmployeeId { get; private set; }

    /// <summary>
    /// Approving authority (head of office or BAC chairperson, depending on disposal method).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? ApprovedByEmployeeId { get; private set; }

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

    public static UnserviceablePropertyReport Create(
        string reportNo,
        DateOnly date,
        DisposalMethod disposalMethod,
        string? fundCluster,
        Guid? inspectedByEmployeeId,
        Guid? approvedByEmployeeId,
        string? remarks)
    {
        return new UnserviceablePropertyReport
        {
            Id                    = Guid.NewGuid(),
            ReportNo              = reportNo,
            Date                  = date,
            DisposalMethod        = disposalMethod,
            FundCluster           = fundCluster,
            InspectedByEmployeeId = inspectedByEmployeeId,
            ApprovedByEmployeeId  = approvedByEmployeeId,
            Remarks               = remarks,
            CreatedOnUtc          = DateTimeOffset.UtcNow,
        };
    }
}
