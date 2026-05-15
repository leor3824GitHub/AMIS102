using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Incidents;

public sealed class PropertyIncidentItem : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid ReportId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;
    public decimal SnapshotAcquisitionCost { get; private set; }
    public decimal SnapshotCurrentReplacementCost { get; private set; }
    public Guid? AccountabilityLineId { get; private set; }
    public IncidentItemResolution ItemResolution { get; private set; }
    public DateOnly? ResolvedOn { get; private set; }

    private PropertyIncidentItem() { }

    internal static PropertyIncidentItem Create(
        string tenantId,
        Guid reportId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        decimal acquisitionCost,
        decimal currentReplacementCost,
        Guid? accountabilityLineId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            AssetRegistryId = assetRegistryId,
            Snapshot = snapshot,
            SnapshotAcquisitionCost = acquisitionCost,
            SnapshotCurrentReplacementCost = currentReplacementCost,
            AccountabilityLineId = accountabilityLineId,
            ItemResolution = IncidentItemResolution.Pending
        };

    internal void Resolve(IncidentItemResolution resolution, DateOnly resolvedOn)
    {
        if (ItemResolution != IncidentItemResolution.Pending)
            throw new InvalidOperationException($"Item already resolved as '{ItemResolution}'.");
        ItemResolution = resolution;
        ResolvedOn = resolvedOn;
    }
}

