using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// Posts COA straight-line depreciation for all PPE assets up to (and including) the month of
/// <see cref="AsOfPeriod"/>. Defaults to the current month. Catches up any unposted months and is
/// idempotent — re-running for the same period posts nothing new. SE assets are never depreciated.
/// </summary>
public sealed record RunDepreciationCommand(DateOnly? AsOfPeriod = null) : ICommand<RunDepreciationResultDto>;

public sealed record RunDepreciationResultDto(
    DateOnly Period,
    int AssetsProcessed,
    int EntriesPosted,
    decimal TotalCharged);

// ── PPE Ledger Card (PPELC) ──────────────────────────────────────────────────

public sealed record DepreciationEntryDto(
    DateOnly Period,
    decimal Amount,
    decimal AccumulatedDepreciationAfter,
    decimal CarryingAmountAfter,
    DateTimeOffset PostedOnUtc);

public sealed record PpeLedgerCardDto(
    Guid AssetRegistryId,
    string PropertyNo,
    string Description,
    AssetType AssetType,
    DateOnly AcquisitionDate,
    decimal AcquisitionCost,
    decimal ResidualValue,
    int EstimatedUsefulLifeYears,
    decimal MonthlyDepreciation,
    DateOnly DepreciationStartDate,
    DateOnly? DepreciatedThrough,
    decimal AccumulatedDepreciation,
    decimal AccumulatedImpairmentLosses,
    decimal CarryingAmount,
    bool IsFullyDepreciated,
    IReadOnlyCollection<DepreciationEntryDto> Entries);

public sealed record GetPpeLedgerCardQuery(string PropertyNo) : IQuery<PpeLedgerCardDto?>;
