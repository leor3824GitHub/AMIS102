using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetRegister.Domain.Repairs;

/// <summary>
/// RPRI (NFA Exhibit 6 — Request for Pre/Post Repair Inspection) lifecycle. Pre-repair inspection is
/// MANDATORY for every PPE repair; the procurement/finance chain (PO-JO + BUR/DV) is optional and only
/// captured as fill-in numbers on the post-repair section. An accepted RPRI is locked in place and
/// becomes the asset's repair-history entry.
/// </summary>
public enum RepairStatus
{
    Requested = 0,
    PreInspected = 1,
    Repaired = 2,
    PostInspected = 3,
    Accepted = 4
}

/// <summary>
/// PPE-wide repair record (RPRI / Exhibit 6), keyed by <see cref="AssetRegistryId"/>. One aggregate per
/// repair request. People are captured as printed names (fill-in form), not employee references.
/// </summary>
public sealed class PropertyRepair : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public Guid AssetRegistryId { get; private set; }
    public string RpriNo { get; private set; } = default!;
    public RepairStatus Status { get; private set; }

    // Defects / complaints (request)
    public string NatureOfWork { get; private set; } = default!;
    public string? PartsToReplace { get; private set; }
    public string RequestedBy { get; private set; } = default!;
    public DateOnly RequestedOn { get; private set; }

    // Inspector assigned to perform the (mandatory) pre-repair inspection. Selected on the request so the
    // person can be notified; denormalized name kept for display without a join. Null when unassigned.
    public Guid? InspectorId { get; private set; }
    public string? InspectorName { get; private set; }

    // Vehicle-specific optional fields (blank for non-vehicle PPE)
    public string? EngineNo { get; private set; }
    public string? ChassisNo { get; private set; }
    public int? OdometerReading { get; private set; }

    // Auto-filled from the latest accepted RPRI of the same asset ("determined upon request")
    public string? NatureOfLastRepair { get; private set; }
    public DateOnly? DateOfLastRepair { get; private set; }

    // Pre-repair inspection (MANDATORY)
    public string? PreInspectionFindings { get; private set; }
    public string? PreInspectedBy { get; private set; }
    public string? NotedBy { get; private set; }
    public DateOnly? PreInspectedOn { get; private set; }

    // Post-repair inspection
    public string? RepairShop { get; private set; }
    public string? JobOrderNo { get; private set; }
    public string? InvoiceNo { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public decimal? AmountPerJO { get; private set; }
    public string? PostInspectionFindings { get; private set; }
    public string? PostInspectedBy { get; private set; }
    public DateOnly? PostInspectedOn { get; private set; }

    // Procurement / finance references (optional — outsourced only)
    public string? PrNo { get; private set; }
    public string? PoJoNo { get; private set; }
    public string? BurNo { get; private set; }
    public string? DvNo { get; private set; }

    // Custodian acceptance (lock-in-place)
    public string? AcceptedBy { get; private set; }
    public DateOnly? AcceptedOn { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyRepair() { }

    public static PropertyRepair Request(
        string tenantId,
        Guid assetRegistryId,
        string rpriNo,
        string natureOfWork,
        string? partsToReplace,
        string requestedBy,
        DateOnly requestedOn,
        string? engineNo,
        string? chassisNo,
        int? odometerReading,
        string? natureOfLastRepair,
        DateOnly? dateOfLastRepair,
        Guid? inspectorId = null,
        string? inspectorName = null,
        string? notedBy = null)
    {
        if (assetRegistryId == Guid.Empty)
            throw new ArgumentException("AssetRegistryId is required.", nameof(assetRegistryId));
        if (string.IsNullOrWhiteSpace(natureOfWork))
            throw new ArgumentException("Nature of work is required.", nameof(natureOfWork));

        return new PropertyRepair
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssetRegistryId = assetRegistryId,
            RpriNo = rpriNo,
            Status = RepairStatus.Requested,
            NatureOfWork = natureOfWork,
            PartsToReplace = partsToReplace,
            RequestedBy = requestedBy,
            RequestedOn = requestedOn,
            InspectorId = inspectorId,
            InspectorName = inspectorName,
            NotedBy = notedBy,
            EngineNo = engineNo,
            ChassisNo = chassisNo,
            OdometerReading = odometerReading,
            NatureOfLastRepair = natureOfLastRepair,
            DateOfLastRepair = dateOfLastRepair,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Pre-repair inspection — MANDATORY to leave Requested. The inspector and "Noted By" head of office
    /// are captured on the request, so they are not supplied here.
    /// </summary>
    public void RecordPreRepairInspection(string findings, string preInspectedBy, DateOnly inspectedOn)
    {
        if (Status != RepairStatus.Requested)
            throw new InvalidOperationException("Pre-repair inspection can only be recorded on a Requested RPRI.");
        if (string.IsNullOrWhiteSpace(findings))
            throw new ArgumentException("Pre-repair inspection findings are required.", nameof(findings));

        PreInspectionFindings = findings;
        PreInspectedBy = preInspectedBy;
        PreInspectedOn = inspectedOn;
        Status = RepairStatus.PreInspected;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordPostRepairInspection(
        string? repairShop, string? jobOrderNo, string? invoiceNo, DateOnly? invoiceDate, decimal? amountPerJO,
        string findings, string postInspectedBy, DateOnly inspectedOn,
        string? prNo, string? poJoNo, string? burNo, string? dvNo)
    {
        if (Status is not (RepairStatus.PreInspected or RepairStatus.Repaired))
            throw new InvalidOperationException("Post-repair inspection requires a pre-inspected (or repaired) RPRI.");
        if (string.IsNullOrWhiteSpace(findings))
            throw new ArgumentException("Post-repair inspection findings are required.", nameof(findings));

        RepairShop = repairShop;
        JobOrderNo = jobOrderNo;
        InvoiceNo = invoiceNo;
        InvoiceDate = invoiceDate;
        AmountPerJO = amountPerJO;
        PostInspectionFindings = findings;
        PostInspectedBy = postInspectedBy;
        PostInspectedOn = inspectedOn;
        PrNo = prNo;
        PoJoNo = poJoNo;
        BurNo = burNo;
        DvNo = dvNo;
        Status = RepairStatus.PostInspected;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Property custodian accepts — lock-in-place; this RPRI is now the history entry.</summary>
    public void Accept(string acceptedBy, DateOnly acceptedOn)
    {
        if (Status != RepairStatus.PostInspected)
            throw new InvalidOperationException("Only a post-inspected RPRI can be accepted.");

        AcceptedBy = acceptedBy;
        AcceptedOn = acceptedOn;
        Status = RepairStatus.Accepted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    internal void SetCreatedBy(string? userId) => CreatedBy = userId;
    internal void SetLastModifiedBy(string? userId) => LastModifiedBy = userId;
}
