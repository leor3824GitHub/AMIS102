using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using Mediator;

namespace FSH.Modules.AssetRegister.Contracts.v1.Accountability;

public sealed record EmployeeRefDto(Guid EmployeeId, string PrintedName, string? Designation);

public sealed record AssetSnapshotDto(
    string PropertyNo,
    string Description,
    AssetType AssetType,
    decimal UnitCost,
    string Unit,
    int EstimatedUsefulLifeYears,
    DateOnly AcquisitionDate,
    string? UacsObjectCode,
    string? SerialNo,
    string? Brand,
    string? Model);

public sealed record VehicleAccountabilityProfileDto(
    int? OdometerAtIssue,
    int? OdometerAtReturn,
    string? PlateNumber,
    string? EngineNumber,
    string? ChassisNumber);

public sealed record PropertyAccountabilityLineDto(
    Guid Id,
    Guid AccountabilityId,
    Guid AssetRegistryId,
    AssetSnapshotDto Snapshot,
    string SnapshotItemNo,
    string? SnapshotResponsibilityCenterCode,
    int IssuedQty,
    int ReturnedQty,
    AccountabilityLineStatus LineStatus,
    DateOnly? ReturnedOn,
    AssetCondition? ReturnedConditionAtReturn,
    Guid? LostOnIncidentId,
    VehicleAccountabilityProfileDto? VehicleProfile);

public sealed record PropertyAccountabilityDto(
    Guid Id,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    string FundCluster,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    AccountabilityStatus Status,
    string? CancellationReason,
    Guid? SupersededByAccountabilityId,
    Guid? SupersedesAccountabilityId,
    EmployeeRefDto IssuedBy,
    EmployeeRefDto ReceivedBy,
    IReadOnlyCollection<PropertyAccountabilityLineDto> Lines);

public sealed record PropertyAccountabilitySummaryDto(
    Guid Id,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    AccountabilityStatus Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    int LineCount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record IssueAccountabilityLineRequest(
    Guid AssetRegistryId,
    string ItemNo,
    Guid LocationId,
    string? ResponsibilityCenterCode,
    int? OdometerAtIssue,
    string? PlateNumber,
    string? EngineNumber,
    string? ChassisNumber);

/// <summary>
/// Issues an ICS (for SE assets) or PAR (for PPE assets). DocumentNo is auto-minted
/// by IAccountabilityNumberGenerator. SE↔ICS / PPE↔PAR enforced at validator AND domain.
/// </summary>
public sealed record IssueAccountabilityCommand(
    AccountabilityType AccountabilityType,
    string FundCluster,
    EmployeeRefDto IssuedBy,
    EmployeeRefDto ReceivedBy,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    IReadOnlyList<IssueAccountabilityLineRequest> Lines) : ICommand<PropertyAccountabilityDto>;

public sealed record RenewAccountabilityCommand(
    Guid AccountabilityId,
    DateOnly NewIssuedOn,
    DateOnly? NewExpiresOn) : ICommand<PropertyAccountabilityDto>;

public sealed record ReturnAccountabilityLineRequest(Guid LineId, int? OdometerAtReturn);

public sealed record ReturnAccountabilityLinesCommand(
    Guid AccountabilityId,
    IReadOnlyList<ReturnAccountabilityLineRequest> Lines,
    DateOnly ReturnedOn,
    AssetCondition ConditionAtReturn) : ICommand<PropertyAccountabilityDto>;

public sealed record CancelAccountabilityCommand(Guid AccountabilityId, string Reason) : ICommand<PropertyAccountabilityDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetAccountabilityQuery(Guid Id) : IQuery<PropertyAccountabilityDto?>;

public sealed record SearchAccountabilitiesQuery(
    string? Keyword = null,
    AccountabilityType? Type = null,
    AccountabilityStatus? Status = null,
    Guid? ReceivedByEmployeeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyAccountabilitySummaryDto>>;
