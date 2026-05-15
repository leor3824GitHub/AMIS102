using AMIS.Framework.Core.Domain;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

namespace AMIS.Modules.AssetRegister.Domain.Accountability;

public sealed class PropertyAccountabilityLine : IHasTenant
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public Guid AccountabilityId { get; private set; }
    public Guid AssetRegistryId { get; private set; }
    public AssetSnapshot Snapshot { get; private set; } = default!;
    public string SnapshotItemNo { get; private set; } = default!;
    public string? SnapshotResponsibilityCenterCode { get; private set; }
    public int IssuedQty { get; private set; }
    public int ReturnedQty { get; private set; }
    public AccountabilityLineStatus LineStatus { get; private set; }
    public DateOnly? ReturnedOn { get; private set; }
    public AssetCondition? ReturnedConditionAtReturn { get; private set; }
    public Guid? LostOnIncidentId { get; private set; }

    public VehicleAccountabilityProfile? VehicleProfile { get; private set; }

    private PropertyAccountabilityLine() { }

    internal static PropertyAccountabilityLine Create(
        string tenantId,
        Guid accountabilityId,
        Guid assetRegistryId,
        AssetSnapshot snapshot,
        string snapshotItemNo,
        string? snapshotResponsibilityCenterCode,
        VehicleAccountabilityProfile? vehicleProfile = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountabilityId = accountabilityId,
            AssetRegistryId = assetRegistryId,
            Snapshot = snapshot,
            SnapshotItemNo = snapshotItemNo,
            SnapshotResponsibilityCenterCode = snapshotResponsibilityCenterCode,
            IssuedQty = 1,
            ReturnedQty = 0,
            LineStatus = AccountabilityLineStatus.Active,
            VehicleProfile = vehicleProfile
        };

    internal void MarkReturned(DateOnly returnedOn, AssetCondition conditionAtReturn, int? odometerAtReturn = null)
    {
        if (LineStatus != AccountabilityLineStatus.Active)
            throw new InvalidOperationException("Only active accountability lines may be returned.");

        VehicleProfile?.RecordReturn(odometerAtReturn);

        LineStatus = AccountabilityLineStatus.Returned;
        ReturnedQty = 1;
        ReturnedOn = returnedOn;
        ReturnedConditionAtReturn = conditionAtReturn;
    }

    internal void MarkLost(Guid incidentReportId)
    {
        if (LineStatus != AccountabilityLineStatus.Active)
            throw new InvalidOperationException("Only active accountability lines may be reported lost.");

        LineStatus = AccountabilityLineStatus.Lost;
        LostOnIncidentId = incidentReportId;
    }
}

