using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Report of Lost/Stolen/Damaged/Destroyed Semi-Expendable Property (RLSDDSP) —
/// COA Circular 2022-004 Annex A.5.
///
/// Documents when tracked property is lost, stolen, damaged, or destroyed.
/// Creating this report sets the referenced SemiExpendableProperty units to
/// PropertyStatus.LostStolenDamaged.
///
/// NOTE: If the property was Issued, the ICS is NOT cancelled by this report —
/// the accountable employee remains liable until formally relieved through the
/// appropriate administrative or legal process.
/// </summary>
public sealed class PropertyIncidentReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// Control number. Suggested format: RLS-YYYY-MM-NNNN
    /// (e.g. RLS-2024-01-0001).
    /// </summary>
    public string ReportNo { get; private set; } = default!;

    /// <summary>Date the report was prepared.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Date the loss / damage / destruction occurred.</summary>
    public DateOnly? IncidentDate { get; private set; }

    /// <summary>Nature of the incident: Lost, Stolen, Damaged, or Destroyed.</summary>
    public PropertyIncidentType IncidentType { get; private set; }

    public string? FundCluster { get; private set; }

    /// <summary>
    /// The employee accountable for the property at the time of incident.
    /// Null if the property was OnHand (supply officer is accountable in that case).
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? AccountableEmployeeId { get; private set; }

    /// <summary>Narrative description of how the loss/damage occurred.</summary>
    public string IncidentDetails { get; private set; } = default!;

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

    public static PropertyIncidentReport Create(
        string tenantId,
        string reportNo,
        DateOnly date,
        DateOnly? incidentDate,
        PropertyIncidentType incidentType,
        string? fundCluster,
        Guid? accountableEmployeeId,
        string incidentDetails,
        string? remarks)
    {
        return new PropertyIncidentReport
        {
            Id                    = Guid.NewGuid(),
            TenantId              = tenantId,
            ReportNo              = reportNo,
            Date                  = date,
            IncidentDate          = incidentDate,
            IncidentType          = incidentType,
            FundCluster           = fundCluster,
            AccountableEmployeeId = accountableEmployeeId,
            IncidentDetails       = incidentDetails,
            Remarks               = remarks,
            CreatedOnUtc          = DateTimeOffset.UtcNow,
        };
    }
}
