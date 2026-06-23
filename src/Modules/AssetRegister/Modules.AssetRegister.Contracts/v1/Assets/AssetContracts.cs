using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Assets;

public sealed record AssetRegistryDto(
    Guid Id,
    string PropertyNo,
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
    Guid? SourcePurchaseOrderId,
    decimal ResidualValue = 0m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

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

/// <summary>
/// Flat, display-ready projection for the mobile "scan an asset" view. Resolves the asset's current
/// location name and — via its current accountability — the document type/number and the accountable
/// officer (PrintedName/Designation are denormalized on the accountability), so the client renders the
/// whole detail screen from a single call. <see cref="DocumentType"/>, <see cref="DocumentNo"/> and the
/// officer fields are null when the asset is not currently on an accountability document.
/// </summary>
public sealed record AssetScanDetailDto(
    Guid Id,
    string PropertyNo,
    AssetType AssetType,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    string Unit,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    LifecycleState LifecycleState,
    AssetCondition CurrentCondition,
    Guid? CurrentLocationId,
    string? LocationName,
    Guid? CurrentAccountabilityId,
    AccountabilityType? DocumentType,
    string? DocumentNo,
    Guid? AccountableOfficerId,
    string? AccountableOfficerName,
    string? AccountableOfficerDesignation);

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

/// <summary>
/// Per-asset override of the catalog depreciation policy (the enterprise "very flexible" path).
/// Applied prospectively — already-posted depreciation is untouched. PPE only.
/// </summary>
public sealed record UpdateAssetDepreciationCommand(
    Guid AssetRegistryId,
    decimal ResidualValue,
    int EstimatedUsefulLifeYears,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine) : ICommand<AssetRegistryDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetAssetRegistryQuery(Guid Id) : IQuery<AssetRegistryDto?>;

public sealed record GetAssetByPropertyNoQuery(string PropertyNo) : IQuery<AssetRegistryDto?>;

public sealed record GetAssetScanDetailByPropertyNoQuery(string PropertyNo) : IQuery<AssetScanDetailDto?>;

public sealed record SearchAssetsQuery(
    string? Keyword = null,
    string? SerialNo = null,
    AssetType? AssetType = null,
    LifecycleState? LifecycleState = null,
    Guid? CurrentCustodianId = null,
    bool IncludeTransferredOut = false,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<AssetRegistrySummaryDto>>;

