using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability;

internal static class AccountabilityMapper
{
    public static AssetSnapshotDto ToDto(AssetSnapshot s) =>
        new(s.PropertyNo, s.Description, s.AssetType, s.UnitCost, s.Unit,
            s.EstimatedUsefulLifeYears, s.AcquisitionDate, s.UacsObjectCode,
            s.SerialNo, s.Brand, s.Model, s.NetBookValue);

    public static EmployeeRefDto ToDto(EmployeeRef e) =>
        new(e.EmployeeId, e.PrintedName, e.Designation);

    public static VehicleAccountabilityProfileDto? ToDto(VehicleAccountabilityProfile? v) =>
        v is null ? null
            : new VehicleAccountabilityProfileDto(
                v.OdometerAtIssue, v.OdometerAtReturn, v.PlateNumber, v.EngineNumber, v.ChassisNumber);

    public static PropertyAccountabilityLineDto ToDto(PropertyAccountabilityLine l, bool hasImage = false) =>
        new(l.Id, l.AccountabilityId, l.AssetRegistryId, ToDto(l.Snapshot),
            l.SnapshotItemNo, l.SnapshotResponsibilityCenterCode, l.IssuedQty, l.ReturnedQty,
            l.LineStatus, l.ReturnedOn, l.ReturnedConditionAtReturn, l.LostOnIncidentId,
            ToDto(l.VehicleProfile), hasImage);

    public static PropertyAccountabilityDto ToDto(PropertyAccountability a, IReadOnlySet<Guid>? assetsWithImage = null) =>
        new(a.Id, a.DocumentNo, a.AccountabilityType, a.FundCluster, a.IssuedOn, a.ExpiresOn,
            a.Status, a.CancellationReason, a.SupersededByAccountabilityId, a.SupersedesAccountabilityId,
            ToDto(a.IssuedBy), ToDto(a.ReceivedBy),
            a.Lines.Select(l => ToDto(l, assetsWithImage?.Contains(l.AssetRegistryId) == true)).ToList(),
            a.AcceptedOn);
}

