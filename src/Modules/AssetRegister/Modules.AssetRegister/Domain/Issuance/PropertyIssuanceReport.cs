using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Issuance;

public sealed class PropertyIssuanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    public string ReportNo { get; private set; } = default!;
    public IssuanceReportType ReportType { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public DateOnly PeriodFromDate { get; private set; }
    public DateOnly PeriodToDate { get; private set; }
    public IssuanceReportStatus Status { get; private set; }

    public EmployeeRef PreparedBy { get; private set; } = default!;
    public EmployeeRef? CertifiedBy { get; private set; }
    public EmployeeRef? PostedBy { get; private set; }
    public DateOnly? PostedOn { get; private set; }

    private readonly List<PropertyIssuanceReportLine> _lines = [];
    public IReadOnlyCollection<PropertyIssuanceReportLine> Lines => _lines.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyIssuanceReport() { }

    public static PropertyIssuanceReport CreateDraft(
        string tenantId,
        string reportNo,
        IssuanceReportType reportType,
        string fundCluster,
        DateOnly periodFromDate,
        DateOnly periodToDate,
        EmployeeRef preparedBy)
    {
        ArgumentNullException.ThrowIfNull(preparedBy);
        if (periodFromDate > periodToDate)
            throw new InvalidOperationException("PeriodFromDate must not be after PeriodToDate.");

        return new PropertyIssuanceReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportNo = reportNo,
            ReportType = reportType,
            FundCluster = fundCluster,
            PeriodFromDate = periodFromDate,
            PeriodToDate = periodToDate,
            Status = IssuanceReportStatus.Draft,
            PreparedBy = preparedBy,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddLine(
        Guid accountabilityId,
        Guid accountabilityLineId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        string? responsibilityCenterCode,
        decimal unitCost)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        EnsureDraft();
        _lines.Add(PropertyIssuanceReportLine.Create(
            Id, accountabilityId, accountabilityLineId, assetRegistryId,
            snapshot, responsibilityCenterCode, unitCost));
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Line '{lineId}' not found on this report.");
        _lines.Remove(line);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Post(EmployeeRef certifiedBy, EmployeeRef postedBy, DateOnly postedOn)
    {
        ArgumentNullException.ThrowIfNull(certifiedBy);
        ArgumentNullException.ThrowIfNull(postedBy);
        EnsureDraft();

        if (_lines.Count == 0)
            throw new InvalidOperationException("Issuance report must include at least one line before posting.");

        var expectedAssetType = ReportType == IssuanceReportType.SMIR ? AssetType.SE : AssetType.PPE;
        if (_lines.Any(l => l.Snapshot.AssetType != expectedAssetType))
            throw new InvalidOperationException(
                $"Report type {ReportType} requires all lines to be {expectedAssetType}.");

        CertifiedBy = certifiedBy;
        PostedBy = postedBy;
        PostedOn = postedOn;
        Status = IssuanceReportStatus.Posted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new IssuanceReportPostedEvent(Id, ReportNo, ReportType, TenantId));
    }

    private void EnsureDraft()
    {
        if (Status != IssuanceReportStatus.Draft)
            throw new InvalidOperationException("Only Draft issuance reports may be modified. Posted reports are immutable.");
    }
}

