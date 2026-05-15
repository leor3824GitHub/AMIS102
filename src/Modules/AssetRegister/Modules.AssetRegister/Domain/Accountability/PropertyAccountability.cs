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
    public Guid? SupersededByAccountabilityId { get; private set; }
    public Guid? SupersedesAccountabilityId { get; private set; }

    public EmployeeRef IssuedBy { get; private set; } = default!;
    public EmployeeRef ReceivedBy { get; private set; } = default!;

    private readonly List<PropertyAccountabilityLine> _lines = [];
    public IReadOnlyCollection<PropertyAccountabilityLine> Lines => _lines.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyAccountability() { }

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

        if (type == AccountabilityType.SE_ICS)
        {
            if (expiresOn is null)
                throw new InvalidOperationException("ICS accountability requires an ExpiresOn date.");
        }
        else if (expiresOn is not null)
        {
            throw new InvalidOperationException("PAR accountability does not carry an ExpiresOn date.");
        }

        var accountability = new PropertyAccountability
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentNo = documentNo,
            AccountabilityType = type,
            FundCluster = fundCluster,
            IssuedOn = issuedOn,
            ExpiresOn = expiresOn,
            Status = AccountabilityStatus.Active,
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
            ExpiresOn = newExpiresOn,
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

