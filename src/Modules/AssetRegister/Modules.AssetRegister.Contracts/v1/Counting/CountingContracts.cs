using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Counting;

public sealed record PhysicalCountEntryDto(
    Guid Id,
    Guid SessionId,
    Guid? AssetRegistryId,
    AssetSnapshotDto? Snapshot,
    string SnapshotArticle,
    string SnapshotUnit,
    decimal SnapshotUnitCost,
    PhysicalCountCondition Condition,
    DateTimeOffset? ScannedOnUtc,
    string? PhotoPath,
    Guid? ScannedByEmployeeId,
    Guid LocationId,
    string? Remarks,
    string? ProposedPropertyClass,
    string? ProposedCategoryCode,
    DateOnly? ProposedAcquisitionDate,
    decimal? ProposedUnitCost,
    bool NeedsRecount,
    string? RecountReason,
    string? ProposedPropertyNo = null,
    Guid? ProposedCatalogItemId = null);

public sealed record PhysicalCountSessionDto(
    Guid Id,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    string FundCluster,
    DateOnly StartedOn,
    DateOnly? ClosedOn,
    DateOnly AsAt,
    string? Remarks,
    string? OfficeOrderNo,
    DateTimeOffset? FrozenOnUtc,
    IReadOnlyCollection<EmployeeRefDto> ConductedBy,
    EmployeeRefDto? ApprovedBy,
    EmployeeRefDto? WitnessedBy,
    IReadOnlyCollection<PhysicalCountEntryDto> Entries);

public sealed record PhysicalCountSessionSummaryDto(
    Guid Id,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    DateOnly AsAt,
    DateOnly StartedOn,
    DateOnly? ClosedOn,
    int EntryCount,
    string? OfficeOrderNo,
    DateTimeOffset? FrozenOnUtc,
    bool HasSignedCopy = false,
    string? FundCluster = null,
    // Per-session breakdown so list clients (e.g. mobile) can show real progress without loading entries.
    int FoundCount = 0,
    int MissingCount = 0,
    int FoundAtStationCount = 0);

// ── Reconciliation read model ──────────────────────────────────────────────

public enum ReconciliationRowStatus
{
    Matched = 0,
    Shortage = 1,
    Overage = 2,
    /// <summary>In-scope registry asset with no count entry — a shortage candidate awaiting MarkMissing or recording.</summary>
    Uncounted = 3
}

public sealed record ReconciliationRowDto(
    Guid? EntryId,
    Guid? AssetRegistryId,
    string? PropertyNo,
    string Article,
    string Unit,
    decimal UnitCost,
    int BookQty,
    int CountedQty,
    ReconciliationRowStatus RowStatus,
    PhysicalCountCondition? Condition,
    bool NeedsRecount,
    string? RecountReason,
    Guid? LocationId,
    string? Remarks);

public sealed record ReconciliationReportDto(
    Guid SessionId,
    string Code,
    PhysicalCountStatus Status,
    string FundCluster,
    PhysicalCountScope Scope,
    string? OfficeOrderNo,
    DateTimeOffset? FrozenOnUtc,
    IReadOnlyList<ReconciliationRowDto> Rows,
    int MatchedCount,
    int ShortageCount,
    int OverageCount,
    int UncountedCount,
    decimal ShortageValue,
    decimal OverageValue);

// ── Count checklist (coverage worklist) read model ──────────────────────────

/// <summary>
/// One in-scope asset on a session's coverage checklist, tagged with whether it has been counted.
/// Status: "Counted" (a good/repair/unserviceable entry exists), "Missing" (marked missing), or
/// "Uncounted" (no entry yet — the field worklist target). LocationName / AccountableOfficer are
/// resolved so mobile can filter and display without extra lookups.
/// </summary>
public sealed record PhysicalCountChecklistItemDto(
    Guid AssetRegistryId,
    string PropertyNo,
    AssetType AssetType,
    string Description,
    string Unit,
    decimal UnitCost,
    Guid? LocationId,
    string? LocationName,
    Guid? CustodianId,
    string? AccountableOfficer,
    string Status,
    PhysicalCountCondition? Condition,
    // Asset photo (base64 data URL or absolute URL); null when no photo has been set. Lets the mobile
    // worklist show a thumbnail so field staff can eyeball-match the item they're looking for.
    string? ImageUrl = null);

/// <summary>
/// The full in-scope asset universe for a session (same fund cluster + scope the reconciliation
/// report uses), each row tagged counted/missing/uncounted. Returned whole so clients can filter
/// (location, officer, PPE/SE), group and cache offline without further round-trips.
/// </summary>
public sealed record PhysicalCountChecklistDto(
    Guid SessionId,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    string FundCluster,
    int TotalCount,
    int CountedCount,
    int MissingCount,
    int UncountedCount,
    IReadOnlyList<PhysicalCountChecklistItemDto> Items);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record StartPhysicalCountCommand(
    string Code,
    PhysicalCountScope Scope,
    string FundCluster,
    DateOnly AsAt,
    DateOnly StartedOn,
    IReadOnlyList<EmployeeRefDto> ConductedBy,
    string? Remarks,
    string? OfficeOrderNo = null) : ICommand<PhysicalCountSessionDto>;

public sealed record FreezePhysicalCountCommand(
    Guid SessionId,
    string OfficeOrderNo) : ICommand<PhysicalCountSessionDto>;

public sealed record RequestPhysicalCountRecountCommand(
    Guid SessionId,
    Guid EntryId,
    string? Reason) : ICommand<PhysicalCountSessionDto>;

public sealed record RecordPhysicalCountEntryCommand(
    Guid SessionId,
    Guid AssetRegistryId,
    string Article,
    string Unit,
    decimal UnitCost,
    PhysicalCountCondition Condition,
    Guid LocationId,
    DateTimeOffset? ScannedOnUtc,
    Guid? ScannedByEmployeeId,
    string? PhotoPath,
    string? Remarks) : ICommand<PhysicalCountSessionDto>;

public sealed record AddFoundAtStationEntryCommand(
    Guid SessionId,
    string Article,
    string Unit,
    decimal UnitCost,
    Guid LocationId,
    string? ProposedPropertyClass,
    string? ProposedCategoryCode,
    DateOnly? ProposedAcquisitionDate,
    decimal? ProposedUnitCost,
    Guid? ScannedByEmployeeId,
    string? Remarks,
    // Operator-assigned identity that lets close auto-materialize the item into the registry.
    string? ProposedPropertyNo = null,
    Guid? ProposedCatalogItemId = null) : ICommand<PhysicalCountSessionDto>;

public sealed record MarkPhysicalCountMissingCommand(
    Guid SessionId,
    Guid AssetRegistryId,
    Guid LocationId,
    string? Remarks) : ICommand<PhysicalCountSessionDto>;

public sealed record ReconcilePhysicalCountCommand(Guid SessionId) : ICommand<PhysicalCountSessionDto>;

public sealed record ClosePhysicalCountCommand(
    Guid SessionId,
    EmployeeRefDto ApprovedBy,
    EmployeeRefDto? WitnessedBy,
    DateOnly ClosedOn,
    string? Station = null) : ICommand<PhysicalCountSessionDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetPhysicalCountSessionQuery(Guid Id) : IQuery<PhysicalCountSessionDto?>;

/// <summary>The in-scope coverage checklist for a session (all assets + counted status).</summary>
public sealed record GetPhysicalCountChecklistQuery(Guid SessionId) : IQuery<PhysicalCountChecklistDto?>;

public sealed record GetReconciliationReportQuery(Guid SessionId) : IQuery<ReconciliationReportDto?>;

public sealed record SearchPhysicalCountSessionsQuery(
    string? Keyword = null,
    PhysicalCountStatus? Status = null,
    PhysicalCountScope? Scope = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PhysicalCountSessionSummaryDto>>;

