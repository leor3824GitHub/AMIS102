using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Accountability;

public sealed class PropertyAccountability : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    public string DocumentNo { get; private set; } = default!;
    public AccountabilityType AccountabilityType { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public DateOnly IssuedOn { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public AccountabilityStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Date the receiving employee accepted the issuance, moving it from
    /// PendingAcceptance to Active. Null while still pending.</summary>
    public DateOnly? AcceptedOn { get; private set; }
    public Guid? SupersededByAccountabilityId { get; private set; }
    public Guid? SupersedesAccountabilityId { get; private set; }

    public EmployeeRef IssuedBy { get; private set; } = default!;
    public EmployeeRef ReceivedBy { get; private set; } = default!;

    /// <summary>COA renewal cadence for PAR (PPE) accountabilities — expires 3 years after issuance.</summary>
    public const int ParRenewalYears = 3;

    private readonly List<PropertyAccountabilityLine> _lines = [];
    public IReadOnlyCollection<PropertyAccountabilityLine> Lines => _lines.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyAccountability() { }

    // Both ICS and PAR carry an expiry date. ICS uses the caller-supplied expiry; PAR is
    // server-authoritative — always issued + 3 years, never user-set (COA renewal cadence).
    private static DateOnly ResolveExpiry(AccountabilityType type, DateOnly issuedOn, DateOnly? requestedExpiresOn)
    {
        if (type == AccountabilityType.PPE_PAR)
            return issuedOn.AddYears(ParRenewalYears);

        return requestedExpiresOn
            ?? throw new InvalidOperationException("ICS accountability requires an expiry date.");
    }

    public static PropertyAccountability Issue(
        string tenantId,
        AccountabilityType type,
        string documentNo,
        string fundCluster,
        EmployeeRef issuedBy,
        EmployeeRef receivedBy,
        DateOnly issuedOn,
        DateOnly? expiresOn,
        IEnumerable<(AssetRegistry asset, string itemNo, string? responsibilityCenterCode, VehicleAccountabilityProfile? vehicleProfile)> lines)
    {
        ArgumentNullException.ThrowIfNull(issuedBy);
        ArgumentNullException.ThrowIfNull(receivedBy);
        ArgumentNullException.ThrowIfNull(lines);

        var materialized = lines.ToList();
        if (materialized.Count == 0)
            throw new InvalidOperationException("Accountability must include at least one line.");

        // Rule #1: form segregation. SE flows on ICS, PPE flows on PAR.
        var expectedAssetType = type == AccountabilityType.SE_ICS ? AssetType.SE : AssetType.PPE;
        foreach (var (asset, _, _, _) in materialized)
        {
            if (asset.AssetType != expectedAssetType)
                throw new InvalidOperationException(
                    $"Asset '{asset.PropertyNo.Value}' is {asset.AssetType} but accountability is {type}. " +
                    "COA forbids mixing SE and PPE on the same accountability document.");
            if (asset.LifecycleState != LifecycleState.Available)
                throw new InvalidOperationException(
                    $"Asset '{asset.PropertyNo.Value}' is in state '{asset.LifecycleState}'. " +
                    "Only Available assets may be issued.");
        }

        var resolvedExpiresOn = ResolveExpiry(type, issuedOn, expiresOn);

        var accountability = new PropertyAccountability
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentNo = documentNo,
            AccountabilityType = type,
            FundCluster = fundCluster,
            IssuedOn = issuedOn,
            ExpiresOn = resolvedExpiresOn,
            // Issued documents await the accountable person's acceptance before they take force.
            Status = AccountabilityStatus.PendingAcceptance,
            IssuedBy = issuedBy,
            ReceivedBy = receivedBy,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        foreach (var (asset, itemNo, rcCode, vehicleProfile) in materialized)
        {
            accountability._lines.Add(PropertyAccountabilityLine.Create(
                tenantId, accountability.Id, asset.Id, asset.Snapshot(), itemNo, rcCode, vehicleProfile));
            accountability.AddDomainEvent(new AssetIssuedEvent(
                asset.Id, accountability.Id, receivedBy.EmployeeId, tenantId));
        }

        return accountability;
    }

    private void EnsureDraft(string action)
    {
        if (Status != AccountabilityStatus.PendingAcceptance)
            throw new InvalidOperationException(
                $"Cannot {action} accountability '{DocumentNo}' because it is {Status}, not awaiting acceptance.");
    }

    /// <summary>The receiving employee accepts the issuance: PendingAcceptance → Active.</summary>
    public void Accept(DateOnly acceptedOn)
    {
        EnsureDraft("accept");
        Status = AccountabilityStatus.Active;
        AcceptedOn = acceptedOn;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Edit header fields while still awaiting acceptance. Type is immutable (it binds the
    /// document-number series and the SE/PPE form). PAR expiry stays server-authoritative.</summary>
    public void UpdateHeader(string fundCluster, EmployeeRef receivedBy, DateOnly issuedOn, DateOnly? expiresOn)
    {
        EnsureDraft("edit");
        ArgumentNullException.ThrowIfNull(receivedBy);
        FundCluster = fundCluster;
        ReceivedBy = receivedBy;
        IssuedOn = issuedOn;
        ExpiresOn = ResolveExpiry(AccountabilityType, issuedOn, expiresOn);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Adds a line while awaiting acceptance. Caller is responsible for reserving the asset
    /// (<c>AssetRegistry.AssignTo</c>). Returns nothing; throws if the asset is ineligible.</summary>
    public void AddLine(AssetRegistry asset, string itemNo, string? responsibilityCenterCode, VehicleAccountabilityProfile? vehicleProfile)
    {
        EnsureDraft("modify lines on");
        ArgumentNullException.ThrowIfNull(asset);

        var expectedAssetType = AccountabilityType == AccountabilityType.SE_ICS ? AssetType.SE : AssetType.PPE;
        if (asset.AssetType != expectedAssetType)
            throw new InvalidOperationException(
                $"Asset '{asset.PropertyNo.Value}' is {asset.AssetType} but accountability is {AccountabilityType}. " +
                "COA forbids mixing SE and PPE on the same accountability document.");
        if (asset.LifecycleState != LifecycleState.Available)
            throw new InvalidOperationException(
                $"Asset '{asset.PropertyNo.Value}' is in state '{asset.LifecycleState}'. Only Available assets may be added.");
        if (_lines.Any(l => l.AssetRegistryId == asset.Id))
            throw new InvalidOperationException($"Asset '{asset.PropertyNo.Value}' is already on this accountability.");

        _lines.Add(PropertyAccountabilityLine.Create(
            TenantId, Id, asset.Id, asset.Snapshot(), itemNo, responsibilityCenterCode, vehicleProfile));
        AddDomainEvent(new AssetIssuedEvent(asset.Id, Id, ReceivedBy.EmployeeId, TenantId));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Removes a line while awaiting acceptance. Caller is responsible for releasing the asset
    /// (<c>AssetRegistry.ReturnToAvailable</c>). Returns the freed asset id.</summary>
    public Guid RemoveLine(Guid lineId)
    {
        EnsureDraft("modify lines on");
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Accountability line '{lineId}' not found.");
        if (_lines.Count == 1)
            throw new InvalidOperationException(
                "An accountability must keep at least one line — delete the document instead of removing the last line.");
        _lines.Remove(line);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return line.AssetRegistryId;
    }

    /// <summary>Guards a hard delete: only a PendingAcceptance document may be deleted. Caller is
    /// responsible for releasing all reserved assets and removing the aggregate.</summary>
    public void EnsureDeletableDraft() => EnsureDraft("delete");

    public PropertyAccountability Renew(string newDocumentNo, DateOnly newIssuedOn, DateOnly? newExpiresOn)
    {
        if (Status != AccountabilityStatus.Active)
            throw new InvalidOperationException($"Only Active accountabilities may be renewed (current: {Status}).");

        var successor = new PropertyAccountability
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            DocumentNo = newDocumentNo,
            AccountabilityType = AccountabilityType,
            FundCluster = FundCluster,
            IssuedOn = newIssuedOn,
            ExpiresOn = ResolveExpiry(AccountabilityType, newIssuedOn, newExpiresOn),
            Status = AccountabilityStatus.Active,
            SupersedesAccountabilityId = Id,
            IssuedBy = IssuedBy,
            ReceivedBy = ReceivedBy,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        foreach (var line in _lines)
        {
            successor._lines.Add(PropertyAccountabilityLine.Create(
                TenantId, successor.Id, line.AssetRegistryId, line.Snapshot,
                line.SnapshotItemNo, line.SnapshotResponsibilityCenterCode,
                line.VehicleProfile));
        }

        Status = AccountabilityStatus.Renewed;
        SupersededByAccountabilityId = successor.Id;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return successor;
    }

    public void ReturnLines(
        IEnumerable<(Guid LineId, int? OdometerAtReturn)> returns,
        DateOnly returnedOn,
        AssetCondition conditionAtReturn)
    {
        ArgumentNullException.ThrowIfNull(returns);
        if (Status != AccountabilityStatus.Active)
            throw new InvalidOperationException($"Only Active accountabilities may have lines returned (current: {Status}).");

        var byId = returns.ToDictionary(r => r.LineId, r => r.OdometerAtReturn);
        foreach (var line in _lines.Where(l => byId.ContainsKey(l.Id)))
        {
            line.MarkReturned(returnedOn, conditionAtReturn, byId[line.Id]);
            AddDomainEvent(new AssetReturnedEvent(line.AssetRegistryId, Id, TenantId));
        }

        if (_lines.All(l => l.LineStatus != AccountabilityLineStatus.Active))
            Status = AccountabilityStatus.Returned;

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ReportLineLost(Guid lineId, Guid incidentReportId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Accountability line '{lineId}' not found.");
        line.MarkLost(incidentReportId);
        AddDomainEvent(new AssetLostEvent(line.AssetRegistryId, incidentReportId, TenantId));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancellation requires a reason.");
        if (_lines.Any(l => l.LineStatus != AccountabilityLineStatus.Active))
            throw new InvalidOperationException("Cannot cancel an accountability once any line has been returned or lost.");

        Status = AccountabilityStatus.Cancelled;
        CancellationReason = reason;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new AccountabilityCancelledEvent(Id, reason, TenantId));
    }
}

