using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Repairs;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

internal static class RepairMapper
{
    public static PropertyRepairDto ToDto(this PropertyRepair r, AssetRegistry asset) => new(
        r.Id,
        r.AssetRegistryId,
        r.RpriNo,
        r.Status.ToString(),
        asset.PropertyNo.Value,
        asset.Description,
        asset.SerialNo,
        asset.Brand,
        asset.Model,
        asset.AcquisitionDate,
        asset.UnitCost,
        r.NatureOfWork,
        r.PartsToReplace,
        r.RequestedBy,
        r.RequestedOn,
        r.EngineNo,
        r.ChassisNo,
        r.OdometerReading,
        r.NatureOfLastRepair,
        r.DateOfLastRepair,
        r.PreInspectionFindings,
        r.PreInspectedBy,
        r.NotedBy,
        r.PreInspectedOn,
        r.RepairShop,
        r.JobOrderNo,
        r.InvoiceNo,
        r.InvoiceDate,
        r.AmountPerJO,
        r.PostInspectionFindings,
        r.PostInspectedBy,
        r.PostInspectedOn,
        r.PrNo,
        r.PoJoNo,
        r.BurNo,
        r.DvNo,
        r.AcceptedBy,
        r.AcceptedOn);

    public static PropertyRepairSummaryDto ToSummary(this PropertyRepair r, AssetRegistry asset) => new(
        r.Id,
        r.AssetRegistryId,
        r.RpriNo,
        r.Status.ToString(),
        asset.PropertyNo.Value,
        asset.Description,
        r.NatureOfWork,
        r.RequestedOn,
        r.AcceptedOn,
        r.AmountPerJO);
}
