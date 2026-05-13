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
    decimal? ProposedUnitCost);

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
    int EntryCount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record StartPhysicalCountCommand(
    string Code,
    PhysicalCountScope Scope,
    string FundCluster,
    DateOnly AsAt,
    DateOnly StartedOn,
    IReadOnlyList<EmployeeRefDto> ConductedBy,
    string? Remarks) : ICommand<PhysicalCountSessionDto>;

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
    string? Remarks) : ICommand<PhysicalCountSessionDto>;

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
    DateOnly ClosedOn) : ICommand<PhysicalCountSessionDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetPhysicalCountSessionQuery(Guid Id) : IQuery<PhysicalCountSessionDto?>;

public sealed record SearchPhysicalCountSessionsQuery(
    string? Keyword = null,
    PhysicalCountStatus? Status = null,
    PhysicalCountScope? Scope = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PhysicalCountSessionSummaryDto>>;

