using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Incidents;

public sealed class PropertyIncidentReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public byte[] Version { get; set; } = [];

    public string IncidentNo { get; private set; } = default!;
    public PropertyIncidentType IncidentType { get; private set; }
    public DateOnly IncidentDate { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public string DepartmentOffice { get; private set; } = default!;
    public string Circumstances { get; private set; } = default!;

    public EmployeeRef AccountableOfficer { get; private set; } = default!;
    public string AccountableOfficerDesignation { get; private set; } = default!;
    public string? AccountableOfficerGovIdType { get; private set; }
    public string? AccountableOfficerGovIdNo { get; private set; }
    public DateOnly? AccountableOfficerGovIdIssuedOn { get; private set; }
    public EmployeeRef? NotedBy { get; private set; }

    public bool PoliceNotified { get; private set; }
    public string? PoliceStation { get; private set; }
    public DateOnly? PoliceNotifiedOn { get; private set; }
    public string? PoliceBlotterRef { get; private set; }

    public DateOnly? NotarizedOn { get; private set; }
    public string? NotaryDocNo { get; private set; }
    public string? NotaryPageNo { get; private set; }
    public string? NotaryBookNo { get; private set; }
    public string? NotarySeriesOf { get; private set; }

    public PropertyIncidentStatus Status { get; private set; }
    public DateOnly? ReliefRequestedOn { get; private set; }
    public DateOnly? ReliefGrantedOn { get; private set; }
    public string? ReliefGrantedRef { get; private set; }
    public decimal? AmountSettled { get; private set; }
    public DateOnly? SettledOn { get; private set; }
    public DateOnly? RecoveredOn { get; private set; }

    private readonly List<PropertyIncidentItem> _items = [];
    public IReadOnlyCollection<PropertyIncidentItem> Items => _items.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyIncidentReport() { }

    public static PropertyIncidentReport File(
        string tenantId,
        string incidentNo,
        PropertyIncidentType incidentType,
        DateOnly incidentDate,
        string fundCluster,
        string departmentOffice,
        string circumstances,
        EmployeeRef accountableOfficer,
        string accountableOfficerDesignation,
        IEnumerable<(Guid assetRegistryId, AssetSnapshot snapshot, decimal acquisitionCost, decimal currentReplacementCost, Guid? accountabilityLineId)> items)
    {
        ArgumentNullException.ThrowIfNull(accountableOfficer);
        ArgumentNullException.ThrowIfNull(items);

        var materialized = items.ToList();
        if (materialized.Count == 0)
            throw new InvalidOperationException("Incident report must include at least one item.");

        var report = new PropertyIncidentReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IncidentNo = incidentNo,
            IncidentType = incidentType,
            IncidentDate = incidentDate,
            FundCluster = fundCluster,
            DepartmentOffice = departmentOffice,
            Circumstances = circumstances,
            AccountableOfficer = accountableOfficer,
            AccountableOfficerDesignation = accountableOfficerDesignation,
            Status = PropertyIncidentStatus.Filed,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        foreach (var (assetId, snapshot, acquisitionCost, crc, lineId) in materialized)
        {
            report._items.Add(PropertyIncidentItem.Create(report.Id, assetId, snapshot, acquisitionCost, crc, lineId));
            report.AddDomainEvent(new AssetLostEvent(assetId, report.Id, tenantId));
        }
        report.AddDomainEvent(new IncidentReportFiledEvent(report.Id, incidentNo, incidentType, tenantId));
        return report;
    }

    public void NotifyPolice(string station, DateOnly notifiedOn, string blotterRef)
    {
        PoliceNotified = true;
        PoliceStation = station;
        PoliceNotifiedOn = notifiedOn;
        PoliceBlotterRef = blotterRef;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Notarize(DateOnly notarizedOn, string docNo, string pageNo, string bookNo, string seriesOf)
    {
        NotarizedOn = notarizedOn;
        NotaryDocNo = docNo;
        NotaryPageNo = pageNo;
        NotaryBookNo = bookNo;
        NotarySeriesOf = seriesOf;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordRecovery(Guid itemId, DateOnly recoveredOn)
    {
        EnsureNotarized();
        var item = RequireItem(itemId);
        item.Resolve(IncidentItemResolution.Recovered, recoveredOn);
        RecoveredOn = recoveredOn;
        AddDomainEvent(new AssetRecoveredEvent(item.AssetRegistryId, Id, TenantId));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordSettlement(Guid itemId, decimal amount, DateOnly settledOn)
    {
        EnsureNotarized();
        if (amount <= 0)
            throw new InvalidOperationException("Settlement amount must be positive.");
        var item = RequireItem(itemId);
        item.Resolve(IncidentItemResolution.Paid, settledOn);
        AmountSettled = (AmountSettled ?? 0m) + amount;
        SettledOn = settledOn;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void GrantRelief(Guid itemId, DateOnly grantedOn, string decisionRef)
    {
        EnsureNotarized();
        var item = RequireItem(itemId);
        item.Resolve(IncidentItemResolution.ReliefGranted, grantedOn);
        ReliefGrantedOn = grantedOn;
        ReliefGrantedRef = decisionRef;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkDerecognized(Guid itemId, DateOnly recordedOn)
    {
        EnsureNotarized();
        var item = RequireItem(itemId);
        item.Resolve(IncidentItemResolution.Derecognized, recordedOn);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (_items.Any(i => i.ItemResolution == IncidentItemResolution.Pending))
            throw new InvalidOperationException("Cannot close an incident report with pending items.");
        Status = PropertyIncidentStatus.Closed;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private PropertyIncidentItem RequireItem(Guid itemId) =>
        _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Incident item '{itemId}' not found.");

    private void EnsureNotarized()
    {
        if (NotarizedOn is null)
            throw new InvalidOperationException("RLSDDSP must be notarized before any resolution may be recorded.");
    }
}

