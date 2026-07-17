using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Events;

namespace AMIS.Modules.AssetRegister.Domain.Issuance;

/// <summary>
/// Property issuance / transfer document — unified across semi-expendable (SMIR) and
/// PPE (PPEIR). Created atomically: the act of creating the report transfers the listed
/// assets out of this office. Only <see cref="Contracts.v1.LifecycleState.Available"/>
/// assets may be issued; assets with an active ICS/PAR must be returned first.
/// </summary>
public sealed class PropertyIssuanceReport : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISignedCopyHolder
{
    public string TenantId { get; private set; } = default!;

    public string ReportNo { get; private set; } = default!;
    public IssuanceReportType ReportType { get; private set; }
    public string FundCluster { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public IssuanceNature Nature { get; private set; }

    /// <summary>Releasing officer of this office (internal).</summary>
    public EmployeeRef IssuedBy { get; private set; } = default!;

    /// <summary>Approving authority (internal).</summary>
    public EmployeeRef ApprovedBy { get; private set; } = default!;

    /// <summary>Accountable officer of the receiving party (may be external).</summary>
    public EmployeeRef IssuedTo { get; private set; } = default!;
    public string IssuedToOfficeAddress { get; private set; } = default!;

    public string? Remarks { get; private set; }

    private readonly List<PropertyIssuanceReportLine> _lines = [];
    public IReadOnlyCollection<PropertyIssuanceReportLine> Lines => _lines.AsReadOnly();

    /// <summary>The uploaded wet-signed copy of this document of record; null until one is uploaded.</summary>
    public SignedCopy? SignedCopy { get; private set; }

    /// <summary>Attaches or replaces the signed copy (one current copy per document).</summary>
    public void SetSignedCopy(SignedCopy copy) => SignedCopy = copy;

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private PropertyIssuanceReport() { }

    public static PropertyIssuanceReport Create(
        string tenantId,
        string reportNo,
        IssuanceReportType reportType,
        string fundCluster,
        DateOnly date,
        IssuanceNature nature,
        EmployeeRef issuedBy,
        EmployeeRef approvedBy,
        EmployeeRef issuedTo,
        string issuedToOfficeAddress,
        string? remarks)
    {
        ArgumentNullException.ThrowIfNull(issuedBy);
        ArgumentNullException.ThrowIfNull(approvedBy);
        ArgumentNullException.ThrowIfNull(issuedTo);
        if (string.IsNullOrWhiteSpace(issuedToOfficeAddress))
            throw new InvalidOperationException("IssuedToOfficeAddress is required.");

        return new PropertyIssuanceReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportNo = reportNo,
            ReportType = reportType,
            FundCluster = fundCluster,
            Date = date,
            Nature = nature,
            IssuedBy = issuedBy,
            ApprovedBy = approvedBy,
            IssuedTo = issuedTo,
            IssuedToOfficeAddress = issuedToOfficeAddress,
            Remarks = remarks,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Adds a snapshotted line for an asset being transferred. Enforces that the asset's
    /// type matches the report type (SMIR↔SE, PPEIR↔PPE).
    /// </summary>
    public PropertyIssuanceReportLine AddLine(Guid assetRegistryId, AssetSnapshot snapshot, decimal unitCost)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var expectedAssetType = ReportType == IssuanceReportType.SMIR ? AssetType.SE : AssetType.PPE;
        if (snapshot.AssetType != expectedAssetType)
            throw new InvalidOperationException(
                $"Report type {ReportType} requires all lines to be {expectedAssetType}. " +
                $"Asset {snapshot.PropertyNo} is {snapshot.AssetType}.");

        var line = PropertyIssuanceReportLine.Create(
            TenantId, Id, assetRegistryId, _lines.Count + 1, snapshot, unitCost);
        _lines.Add(line);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        return line;
    }

    /// <summary>Raises the issued/posted integration event. Call once after all lines are added.</summary>
    public void MarkIssued() =>
        AddDomainEvent(new IssuanceReportPostedEvent(Id, ReportNo, ReportType, TenantId));

    /// <summary>
    /// Fills accounting depreciation / book value for a PPE line. Only valid on PPEIR reports.
    /// </summary>
    public void SetLineDepreciation(Guid lineId, decimal accumulatedDepreciation, decimal bookValue)
    {
        if (ReportType != IssuanceReportType.PPEIR)
            throw new InvalidOperationException("Depreciation may only be recorded on PPEIR reports.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new InvalidOperationException($"Line '{lineId}' not found on this report.");
        line.SetDepreciation(accumulatedDepreciation, bookValue);
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
