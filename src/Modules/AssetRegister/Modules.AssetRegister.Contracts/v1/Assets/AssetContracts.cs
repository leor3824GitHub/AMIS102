using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Assets;

public sealed record AssetRegistryDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    AssetType AssetType,
    AssetCategory Category,
    string PropertyClass,
    string CategoryCode,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    string Unit,
    string FundCluster,
    string UacsObjectCode,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    int EstimatedUsefulLifeYears,
    decimal AccumulatedDepreciation,
    decimal AccumulatedImpairmentLosses,
    decimal CarryingAmount,
    LifecycleState LifecycleState,
    AssetCondition CurrentCondition,
    Guid? CurrentCustodianId,
    Guid? CurrentLocationId,
    Guid? CurrentAccountabilityId,
    Guid? SourceIARId,
    Guid? SourcePurchaseOrderId);

public sealed record AssetRegistrySummaryDto(
    Guid Id,
    string PropertyNo,
    AssetType AssetType,
    string Description,
    decimal UnitCost,
    DateOnly AcquisitionDate,
    LifecycleState LifecycleState,
    AssetCondition CurrentCondition,
    Guid? CurrentCustodianId);

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// Registers a single physical unit. One command = one row (per cardinality rule §B).
/// PropertyNo is operator-assigned (NFA policy) — must be supplied by the caller.
/// </summary>
public sealed record RegisterAssetCommand(
    Guid CatalogItemId,
    AssetType AssetType,
    AssetCategory Category,
    string PropertyNo,
    string Description,
    string FundCluster,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string? SerialNo = null,
    string? Brand = null,
    string? Model = null,
    Guid? SourceIARId = null,
    Guid? SourcePurchaseOrderId = null) : ICommand<AssetRegistryDto>;

public sealed record UpdateAssetConditionCommand(
    Guid AssetRegistryId,
    AssetCondition Condition) : ICommand<AssetRegistryDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetAssetRegistryQuery(Guid Id) : IQuery<AssetRegistryDto?>;

public sealed record GetAssetByPropertyNoQuery(string PropertyNo) : IQuery<AssetRegistryDto?>;

public sealed record SearchAssetsQuery(
    string? Keyword = null,
    AssetType? AssetType = null,
    LifecycleState? LifecycleState = null,
    Guid? CurrentCustodianId = null,
    Guid? CatalogItemId = null,
    bool IncludeTransferredOut = false,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<AssetRegistrySummaryDto>>;

