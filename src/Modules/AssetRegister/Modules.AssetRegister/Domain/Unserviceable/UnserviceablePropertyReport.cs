using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Unserviceable;

public sealed class UnserviceablePropertyReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISignedCopyHolder
{
    public string TenantId { get; private set; } = default!;

    public string ReportNo { get; private set; } = default!;
    public UnserviceableReportType ReportType { get; private set; }
    public DateOnly AsAt { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public string Station { get; private set; } = default!;
    public UnserviceableReportStatus Status { get; private set; }

    public EmployeeRef AccountableOfficer { get; private set; } = default!;
    public EmployeeRef? ApprovedBy { get; private set; }
    public EmployeeRef? InspectedBy { get; private set; }
    public DateOnly? InspectedOn { get; private set; }
    public EmployeeRef? WitnessedBy { get; private set; }
    public DateOnly? WitnessedOn { get; private set; }

    private readonly List<UnserviceablePropertyItem> _items = [];
    public IReadOnlyCollection<UnserviceablePropertyItem> Items => _items.AsReadOnly();

    /// <summary>The uploaded wet-signed copy of this document of record; null until one is uploaded.</summary>
    public SignedCopy? SignedCopy { get; private set; }

    /// <summary>Attaches or replaces the signed copy (one current copy per document).</summary>
    public void SetSignedCopy(SignedCopy copy) => SignedCopy = copy;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private UnserviceablePropertyReport() { }

    public static UnserviceablePropertyReport CreateDraft(
        string tenantId,
        string reportNo,
        UnserviceableReportType reportType,
        string station,
        DateOnly asAt,
        EmployeeRef accountableOfficer)
    {
        ArgumentNullException.ThrowIfNull(accountableOfficer);
        return new UnserviceablePropertyReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportNo = reportNo,
            ReportType = reportType,
            // Blank until the first item is added, which stamps it from that asset's cluster (see AddItem).
            FundCluster = string.Empty,
            Station = station,
            AsAt = asAt,
            AccountableOfficer = accountableOfficer,
            Status = UnserviceableReportStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Edits the report's header while it is still <see cref="UnserviceableReportStatus.Draft"/>.
    /// ReportType is immutable (it binds the number series and the SE/PPE form) and FundCluster is derived
    /// from the items rather than hand-edited, so neither is a parameter here.</summary>
    public void UpdateHeader(string station, DateOnly asAt, EmployeeRef accountableOfficer)
    {
        ArgumentNullException.ThrowIfNull(accountableOfficer);
        if (Status != UnserviceableReportStatus.Draft)
            throw new InvalidOperationException("Only Draft reports may have their header edited.");

        Station = station;
        AsAt = asAt;
        AccountableOfficer = accountableOfficer;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void AddItem(AssetRegistry asset, string? remarks)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (Status != UnserviceableReportStatus.Draft)
            throw new InvalidOperationException("Items may only be added while the report is Draft.");

        var expected = ReportType == UnserviceableReportType.IIRUSP ? AssetType.SE : AssetType.PPE;
        if (asset.AssetType != expected)
            throw new InvalidOperationException(
                $"Report type {ReportType} requires {expected} assets; received {asset.AssetType}.");

        // A disposal report is filed under exactly one fund cluster. The first item stamps it from the
        // asset (inherited, never typed); every later item must match, so the report can't span clusters.
        if (_items.Count == 0)
            FundCluster = asset.FundCluster;
        else if (!string.Equals(asset.FundCluster, FundCluster, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"This report is under fund cluster '{FundCluster}'; asset {asset.PropertyNo.Value} is under '{asset.FundCluster}'. Items on one report must share a fund cluster.");

        _items.Add(UnserviceablePropertyItem.Create(
            TenantId, Id, asset.Id, asset.Snapshot(),
            asset.AcquisitionDate, asset.UnitCost,
            asset.AccumulatedDepreciation, asset.AccumulatedImpairmentLosses,
            remarks));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Submit(EmployeeRef approvedBy)
    {
        ArgumentNullException.ThrowIfNull(approvedBy);
        if (Status != UnserviceableReportStatus.Draft)
            throw new InvalidOperationException("Only Draft reports may be submitted.");
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot submit an empty unserviceable report.");

        ApprovedBy = approvedBy;
        Status = UnserviceableReportStatus.Submitted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new UnserviceableReportSubmittedEvent(Id, ReportNo, ReportType, TenantId));
    }

    public void RecordInspection(
        EmployeeRef inspectedBy,
        DateOnly inspectedOn,
        EmployeeRef? witnessedBy,
        DateOnly? witnessedOn,
        IEnumerable<(Guid itemId, DisposalMethod method, string? otherSpecify, decimal? appraisedValue)> decisions)
    {
        ArgumentNullException.ThrowIfNull(inspectedBy);
        ArgumentNullException.ThrowIfNull(decisions);
        if (Status != UnserviceableReportStatus.Submitted)
            throw new InvalidOperationException("Inspection can only be recorded after submission.");

        foreach (var (itemId, method, otherSpecify, appraisedValue) in decisions)
        {
            var item = RequireItem(itemId);
            item.RecordInspectionDecision(method, otherSpecify, appraisedValue);
        }

        InspectedBy = inspectedBy;
        InspectedOn = inspectedOn;
        WitnessedBy = witnessedBy;
        WitnessedOn = witnessedOn;
        Status = UnserviceableReportStatus.InspectionDone;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordDisposal(IEnumerable<(Guid itemId, DateOnly disposalRecordedOn, string? saleORNo, decimal? saleAmount)> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (Status != UnserviceableReportStatus.InspectionDone)
            throw new InvalidOperationException("Disposal can only be recorded after inspection.");

        foreach (var (itemId, recordedOn, orNo, amount) in records)
        {
            var item = RequireItem(itemId);
            item.RecordDisposal(recordedOn, orNo, amount);
            AddDomainEvent(new AssetDisposedEvent(
                item.AssetRegistryId, item.Snapshot.PropertyNo,
                item.DisposalMethod!.Value, TenantId));
        }

        Status = UnserviceableReportStatus.DisposalRecorded;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status != UnserviceableReportStatus.DisposalRecorded)
            throw new InvalidOperationException("Report must be DisposalRecorded before it can be Closed.");
        Status = UnserviceableReportStatus.Closed;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private UnserviceablePropertyItem RequireItem(Guid itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Unserviceable item '{itemId}' not found.");
}

