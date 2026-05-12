using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace FSH.Modules.AssetRegister.Domain.Issuance;

public sealed class PropertyIssuanceReportLine
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid AccountabilityId { get; private set; }
    public Guid AccountabilityLineId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;
    public string? SnapshotResponsibilityCenterCode { get; private set; }
    public int SnapshotQuantityIssued { get; private set; }
    public decimal SnapshotUnitCost { get; private set; }
    public decimal SnapshotAmount { get; private set; }

    private PropertyIssuanceReportLine() { }

    internal static PropertyIssuanceReportLine Create(
        Guid reportId,
        Guid accountabilityId,
        Guid accountabilityLineId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        string? responsibilityCenterCode,
        decimal unitCost) =>
        new()
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            AccountabilityId = accountabilityId,
            AccountabilityLineId = accountabilityLineId,
            AssetRegistryId = assetRegistryId,
            Snapshot = snapshot,
            SnapshotResponsibilityCenterCode = responsibilityCenterCode,
            SnapshotQuantityIssued = 1,
            SnapshotUnitCost = unitCost,
            SnapshotAmount = unitCost
        };
}
