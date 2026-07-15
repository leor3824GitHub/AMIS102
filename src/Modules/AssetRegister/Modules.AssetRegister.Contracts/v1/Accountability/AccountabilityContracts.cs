using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

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
    string? Model,
    decimal NetBookValue = 0m);

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
    VehicleAccountabilityProfileDto? VehicleProfile,
    // Whether the underlying asset has a photo. The bytes are NOT in this payload — the mobile
    // client lazily streams the thumbnail from GET /assets/{id}/image using AssetRegistryId.
    // Defaults false so write-op command handlers (Issue/Accept/Cancel/…) need no change.
    bool HasImage = false);

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
    IReadOnlyCollection<PropertyAccountabilityLineDto> Lines,
    DateOnly? AcceptedOn = null);

public sealed record PropertyAccountabilitySummaryDto(
    Guid Id,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    AccountabilityStatus Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    int LineCount,
    bool HasSignedCopy = false);

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

/// <summary>
/// Edits a still-pending (not yet accepted) ICS/PAR: header fields plus the full line set.
/// The handler reconciles assets — lines added reserve their asset, lines removed release it.
/// AccountabilityType and DocumentNo are immutable. Rejected once the document is accepted.
/// </summary>
public sealed record UpdateAccountabilityCommand(
    Guid AccountabilityId,
    string FundCluster,
    EmployeeRefDto ReceivedBy,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    IReadOnlyList<IssueAccountabilityLineRequest> Lines) : ICommand<PropertyAccountabilityDto>;

/// <summary>Hard-deletes a still-pending ICS/PAR and releases its reserved assets back to Available.</summary>
public sealed record DeleteAccountabilityCommand(Guid AccountabilityId) : ICommand<Unit>;

/// <summary>
/// The accountable (ReceivedBy) employee accepts a pending issuance, moving it to Active.
/// Server-scoped to the current employee in the self-service flow.
/// </summary>
public sealed record AcceptAccountabilityCommand(Guid AccountabilityId, DateOnly AcceptedOn) : ICommand<PropertyAccountabilityDto>;

// ── Queries ────────────────────────────────────────────────────────────────

/// <summary>
/// Previews the next ICS / PAR document number for the given type/date WITHOUT consuming it.
/// Scoped to the current tenant. Backs the "next number" preview on the issue dialog.
/// PAR is purely date-based; ICS uses the SPLV (low-valued) or SPHV (high-valued) counter —
/// <paramref name="HighValued"/> selects which, inferred from the selected assets. Best-effort —
/// a concurrent create may change the actual number before save.
/// </summary>
public sealed record PeekAccountabilityNumberQuery(
    AccountabilityType Type,
    DateOnly Date,
    bool HighValued = false) : IQuery<string>;

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

/// <summary>
/// Active ICS/PAR accountabilities whose expiry falls on or before <c>today + WithinDays</c> — i.e. those
/// due (or already overdue) for renewal. Ordered soonest-expiry-first so overdue documents surface at the
/// top of the renewal worklist. Backed by the (TenantId, Status, ExpiresOn) index.
/// </summary>
public sealed record GetExpiringAccountabilitiesQuery(int WithinDays = 60)
    : IQuery<IReadOnlyList<PropertyAccountabilitySummaryDto>>;

// ── Employee self-service ("My Accountability") ──────────────────────────────
// These carry NO employee id — the server resolves the accountable person from the
// authenticated identity, so a user can only ever see their own ICS/PAR.

public sealed record GetMyAccountabilitiesQuery(
    string? Keyword = null,
    AccountabilityType? Type = null,
    AccountabilityStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyAccountabilitySummaryDto>>;

public sealed record GetMyAccountabilityDetailQuery(Guid Id) : IQuery<PropertyAccountabilityDto?>;

