using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Issuance;

public sealed class PropertyIssuanceReportLine : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReportId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public int ItemNo { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;
    public decimal SnapshotUnitCost { get; private set; }
    public decimal SnapshotAmount { get; private set; }

    /// <summary>Null until Accounting fills via UpdateIssuanceReportDepreciation (PPEIR only).</summary>
    public decimal? AccumulatedDepreciation { get; private set; }

    /// <summary>Null until Accounting fills via UpdateIssuanceReportDepreciation (PPEIR only).</summary>
    public decimal? BookValue { get; private set; }

    private PropertyIssuanceReportLine() { }

    internal static PropertyIssuanceReportLine Create(
        string tenantId,
        Guid reportId,
        Guid assetRegistryId,
        int itemNo,
        AssetSnapshot snapshot,
        decimal unitCost) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            AssetRegistryId = assetRegistryId,
            ItemNo = itemNo,
            Snapshot = snapshot,
            SnapshotUnitCost = unitCost,
            SnapshotAmount = unitCost
        };

    public void SetDepreciation(decimal accumulatedDepreciation, decimal bookValue)
    {
        AccumulatedDepreciation = accumulatedDepreciation;
        BookValue = bookValue;
    }
}
