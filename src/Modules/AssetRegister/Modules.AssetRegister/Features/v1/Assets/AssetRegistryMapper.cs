using AMIS.Modules.AssetRegister.Contracts.v1.Assets;
using AMIS.Modules.AssetRegister.Domain.Assets;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets;

internal static class AssetRegistryMapper
{
    public static AssetRegistryDto ToDto(AssetRegistry x) =>
        new(x.Id,
            x.PropertyNo.Value,
            x.AssetType,
            x.Category,
            x.PropertyClass,
            x.CategoryCode,
            x.Description,
            x.SerialNo,
            x.Brand,
            x.Model,
            x.Unit,
            x.FundCluster,
            x.UacsObjectCode,
            x.AcquisitionDate,
            x.UnitCost,
            x.EstimatedUsefulLifeYears,
            x.AccumulatedDepreciation,
            x.AccumulatedImpairmentLosses,
            x.CarryingAmount,
            x.LifecycleState,
            x.CurrentCondition,
            x.CurrentCustodianId,
            x.CurrentLocationId,
            x.CurrentAccountabilityId,
            x.SourceIARId,
            x.SourcePurchaseOrderId);
}

