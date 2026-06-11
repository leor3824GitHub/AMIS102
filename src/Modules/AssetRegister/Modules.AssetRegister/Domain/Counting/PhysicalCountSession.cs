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

    /// <summary>COA office order authorizing the count; required to freeze the ledger.</summary>
    public string? OfficeOrderNo { get; private set; }

    /// <summary>When the ledger freeze took effect. Null on Draft and on legacy sessions started before the freeze workflow.</summary>
    public DateTimeOffset? FrozenOnUtc { get; private set; }

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
        string? remarks,
        string? officeOrderNo = null)
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
            Status = PhysicalCountStatus.Draft,
            FundCluster = fundCluster,
            StartedOn = startedOn,
            AsAt = asAt,
            Remarks = remarks,
            OfficeOrderNo = officeOrderNo,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
        session._conductedBy.AddRange(members);
        return session;
    }

    /// <summary>
    /// Activates the ledger freeze: the session moves Draft → Ongoing, count recording opens,
    /// and asset movements covered by this session's fund cluster + scope are blocked until Close.
    /// </summary>
    public void Freeze(string officeOrderNo, DateTimeOffset frozenOnUtc)
    {
        if (Status != PhysicalCountStatus.Draft)
            throw new InvalidOperationException($"Only a Draft session can be frozen (current: {Status}).");
        if (string.IsNullOrWhiteSpace(officeOrderNo))
            throw new InvalidOperationException("An Office Order No. is required to freeze the ledger.");

        OfficeOrderNo = officeOrderNo;
        FrozenOnUtc = frozenOnUtc;
        Status = PhysicalCountStatus.Ongoing;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new PhysicalCountFrozenEvent(Id, FundCluster, Scope, TenantId));
    }

    /// <summary>
    /// Flags a recorded entry for recount during reconciliation. The flagged entry becomes
    /// re-recordable via RecordEntry while the session stays Reconciled.
    /// </summary>
    public void RequestRecount(Guid entryId, string? reason)
    {
        if (Status != PhysicalCountStatus.Reconciled)
            throw new InvalidOperationException($"Recounts can only be requested while the session is Reconciled (current: {Status}).");
        var entry = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new InvalidOperationException($"Entry '{entryId}' not found on session.");
        entry.MarkForRecount(reason);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new PhysicalCountRecountRequestedEvent(Id, entryId, TenantId));
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
        EnsureScopeAllows(asset.AssetType);

        if (Status == PhysicalCountStatus.Reconciled)
        {
            // During reconciliation only recount-flagged entries may be re-recorded.
            var recountEntry = _entries.FirstOrDefault(e => e.AssetRegistryId == asset.Id && e.NeedsRecount)
                ?? throw new InvalidOperationException(
                    "Re-recording in Reconciled status is only allowed for entries flagged for recount.");
            recountEntry.ApplyRecount(condition, locationId, scannedOnUtc, scannedByEmployeeId, photoPath, remarks);
            LastModifiedOnUtc = DateTimeOffset.UtcNow;
            return;
        }

        EnsureOngoing();
        _entries.Add(PhysicalCountEntry.CreateForKnownAsset(
            TenantId, Id, asset.Id, asset.Snapshot(), article, unit, unitCost,
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
        string? proposedPropertyNo,
        Guid? proposedCatalogItemId,
        Guid? scannedByEmployeeId,
        string? remarks)
    {
        EnsureOngoing();
        _entries.Add(PhysicalCountEntry.CreateFoundAtStation(
            TenantId, Id, article, unit, unitCost, locationId,
            proposedPropertyClass, proposedCategoryCode, proposedAcquisitionDate, proposedUnitCost,
            proposedPropertyNo, proposedCatalogItemId, scannedByEmployeeId, remarks));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkMissing(AssetRegistry asset, Guid locationId, string? remarks)
    {
        ArgumentNullException.ThrowIfNull(asset);
        EnsureOngoing();
        EnsureScopeAllows(asset.AssetType);
        _entries.Add(PhysicalCountEntry.CreateForKnownAsset(
            TenantId, Id, asset.Id, asset.Snapshot(), asset.Description, asset.Unit, asset.UnitCost,
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
        if (_entries.Any(e => e.NeedsRecount))
            throw new InvalidOperationException("Cannot close while entries are awaiting recount.");

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

