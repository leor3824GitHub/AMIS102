using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;

namespace FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum AppStatus
{
    Draft = 0,
    Published = 1,
    Approved = 2,
    Superseded = 3,
    Returned = 4
}

public enum AppPhase
{
    Indicative = 0,
    Final = 1,
    Updated = 2
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record AppItemDto(
    Guid Id,
    Guid SourcePpmpId,
    Guid SourcePpmpItemId,
    string OfficeCode,
    string EndUserUnit,
    int ItemNo,
    string GeneralDescription,
    ProjectType ProjectType,
    decimal Quantity,
    string Unit,
    string ModeOfProcurement,
    bool PreProcurementConference,
    string ProcurementStart,
    string ProcurementEnd,
    string ExpectedDelivery,
    string SourceOfFunds,
    decimal EstimatedBudget,
    string? Remarks);

public sealed record AnnualProcurementPlanDto(
    Guid Id,
    string AppNumber,
    int FiscalYear,
    AppPhase Phase,
    AppStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    Guid? PreviousVersionId,
    string? AmendmentReason,
    DateTimeOffset? AmendedAt,
    Guid? AmendedById,
    Guid? ConsolidatedById,
    DateTimeOffset? ConsolidatedOn,
    Guid? ApprovedById,
    DateTimeOffset? ApprovedOn,
    string? ReturnReason,
    DateTimeOffset? ReturnedAt,
    Guid? ReturnedById,
    decimal TotalEstimatedBudget,
    IReadOnlyList<AppItemDto> Items,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record AnnualProcurementPlanSummaryDto(
    Guid Id,
    string AppNumber,
    int FiscalYear,
    AppPhase Phase,
    AppStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    int ItemCount,
    decimal TotalEstimatedBudget,
    DateTimeOffset CreatedOnUtc);

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreateAnnualProcurementPlanCommand(
    int FiscalYear,
    AppPhase Phase) : ICommand<AnnualProcurementPlanDto>;

public sealed record ConsolidatePpmpsCommand(
    Guid AppId,
    IReadOnlyList<Guid> PpmpIds) : ICommand<AnnualProcurementPlanDto>;

public sealed record PublishAnnualProcurementPlanCommand(Guid Id) : ICommand<AnnualProcurementPlanDto>;

/// <summary>Promotes an Approved Indicative APP to a new Final draft (per-phase version reset to 1).</summary>
public sealed record PromoteToFinalAppCommand(Guid Id) : ICommand<AnnualProcurementPlanDto>;

/// <summary>Creates a new Updated version of an Approved Final or Updated APP.</summary>
public sealed record CreateUpdateAppCommand(Guid Id, string UpdateReason) : ICommand<AnnualProcurementPlanDto>;

public sealed record ApproveAppCommand(Guid Id, Guid ApprovedById) : ICommand<AnnualProcurementPlanDto>;
public sealed record RecallAppCommand(Guid Id) : ICommand<AnnualProcurementPlanDto>;
public sealed record ReturnAppCommand(Guid Id, string ReturnReason, Guid ReturnedById) : ICommand<AnnualProcurementPlanDto>;
public sealed record DeleteAnnualProcurementPlanCommand(Guid Id) : ICommand<Unit>;

// ── Queries ───────────────────────────────────────────────────────────────────

public sealed record GetAnnualProcurementPlanQuery(Guid Id) : IQuery<AnnualProcurementPlanDto>;

public sealed record GetAppVersionsQuery(Guid VersionChainId) : IQuery<IReadOnlyList<AnnualProcurementPlanSummaryDto>>;

public sealed record SearchAnnualProcurementPlansQuery : IQuery<PagedResponse<AnnualProcurementPlanSummaryDto>>
{
    public string? Keyword { get; init; }
    public int? FiscalYear { get; init; }
    public AppStatus? Status { get; init; }
    public AppPhase? Phase { get; init; }
    public bool CurrentVersionOnly { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
