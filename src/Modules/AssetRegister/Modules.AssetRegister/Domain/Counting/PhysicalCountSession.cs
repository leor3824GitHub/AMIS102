using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Counting;

public sealed class PhysicalCountSession : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    public string Code { get; private set; } = default!;
    public PhysicalCountScope Scope { get; private set; }
    public PhysicalCountStatus Status { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public DateOnly StartedOn { get; private set; }
    public DateOnly? ClosedOn { get; private set; }
    public DateOnly AsAt { get; private set; }
    public string? Remarks { get; private set; }

    private readonly List<EmployeeRef> _conductedBy = [];
    public IReadOnlyCollection<EmployeeRef> ConductedBy => _conductedBy.AsReadOnly();
    public EmployeeRef? ApprovedBy { get; private set; }
    public EmployeeRef? WitnessedBy { get; private set; }

    private readonly List<PhysicalCountEntry> _entries = [];
    public IReadOnlyCollection<PhysicalCountEntry> Entries => _entries.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PhysicalCountSession() { }

    public static PhysicalCountSession Start(
        string tenantId,
        string code,
        PhysicalCountScope scope,
        string fundCluster,
        DateOnly asAt,
        DateOnly startedOn,
        IEnumerable<EmployeeRef> conductedBy,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(conductedBy);
        var members = conductedBy.ToList();
        if (members.Count == 0)
            throw new InvalidOperationException("PhysicalCountSession requires at least one inventory committee member.");

        var session = new PhysicalCountSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Scope = scope,
            Status = PhysicalCountStatus.Ongoing,
            FundCluster = fundCluster,
            StartedOn = startedOn,
            AsAt = asAt,
            Remarks = remarks,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
        session._conductedBy.AddRange(members);
        return session;
    }

    public void RecordEntry(
        AssetRegistry asset,
        string article,
        string unit,
        decimal unitCost,
        PhysicalCountCondition condition,
        Guid locationId,
        DateTimeOffset? scannedOnUtc,
        Guid? scannedByEmployeeId,
        string? photoPath,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(asset);
        EnsureOngoing();
        EnsureScopeAllows(asset.AssetType);
        _entries.Add(PhysicalCountEntry.CreateForKnownAsset(
            Id, asset.Id, asset.Snapshot(), article, unit, unitCost,
            condition, locationId, scannedOnUtc, scannedByEmployeeId, photoPath, remarks));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void AddFoundAtStationEntry(
        string article,
        string unit,
        decimal unitCost,
        Guid locationId,
        string? proposedPropertyClass,
        string? proposedCategoryCode,
        DateOnly? proposedAcquisitionDate,
        decimal? proposedUnitCost,
        Guid? scannedByEmployeeId,
        string? remarks)
    {
        EnsureOngoing();
        _entries.Add(PhysicalCountEntry.CreateFoundAtStation(
            Id, article, unit, unitCost, locationId,
            proposedPropertyClass, proposedCategoryCode, proposedAcquisitionDate, proposedUnitCost,
            scannedByEmployeeId, remarks));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkMissing(AssetRegistry asset, Guid locationId, string? remarks)
    {
        ArgumentNullException.ThrowIfNull(asset);
        EnsureOngoing();
        EnsureScopeAllows(asset.AssetType);
        _entries.Add(PhysicalCountEntry.CreateForKnownAsset(
            Id, asset.Id, asset.Snapshot(), asset.Description, asset.Unit, asset.UnitCost,
            PhysicalCountCondition.Missing, locationId, null, null, null, remarks));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Reconcile()
    {
        EnsureOngoing();
        foreach (var entry in _entries.Where(e => e.Condition == PhysicalCountCondition.FoundAtStation))
        {
            AddDomainEvent(new AssetFoundAtStationEvent(Id, entry.Id, TenantId));
        }
        foreach (var entry in _entries.Where(e => e.Condition == PhysicalCountCondition.Missing && e.AssetRegistryId is not null))
        {
            AddDomainEvent(new AssetReportedMissingFromCountEvent(entry.AssetRegistryId!.Value, Id, TenantId));
        }
        Status = PhysicalCountStatus.Reconciled;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Close(EmployeeRef approvedBy, EmployeeRef? witnessedBy, DateOnly closedOn)
    {
        ArgumentNullException.ThrowIfNull(approvedBy);
        if (Status != PhysicalCountStatus.Reconciled)
            throw new InvalidOperationException($"PhysicalCountSession must be Reconciled before Close (current: {Status}).");
        if (_entries.Any(e => e.Condition == PhysicalCountCondition.FoundAtStation && e.AssetRegistryId is null))
            throw new InvalidOperationException("Cannot close while a FoundAtStation entry has not been materialized into the registry.");

        ApprovedBy = approvedBy;
        WitnessedBy = witnessedBy;
        ClosedOn = closedOn;
        Status = PhysicalCountStatus.Closed;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new PhysicalCountSessionClosedEvent(Id, TenantId));
    }

    internal void AttachReconciledAssetToEntry(Guid entryId, Guid assetRegistryId)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException($"Entry '{entryId}' not found on session.");
        entry.AttachReconciledAsset(assetRegistryId);
    }

    private void EnsureOngoing()
    {
        if (Status != PhysicalCountStatus.Ongoing)
            throw new InvalidOperationException($"Operation not allowed in status '{Status}'.");
    }

    private void EnsureScopeAllows(AssetType assetType)
    {
        var allowed = Scope switch
        {
            PhysicalCountScope.Both => true,
            PhysicalCountScope.SEOnly => assetType == AssetType.SE,
            PhysicalCountScope.PPEOnly => assetType == AssetType.PPE,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"AssetType {assetType} is outside session scope {Scope}.");
    }
}

