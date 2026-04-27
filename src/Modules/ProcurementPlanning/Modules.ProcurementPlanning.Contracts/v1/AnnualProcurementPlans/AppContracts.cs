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
    Superseded = 3
}

public enum AppRevisionType
{
    Original = 0,
    Supplemental = 1,
    Revised = 2
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
    ModeOfProcurement ModeOfProcurement,
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
    AppRevisionType RevisionType,
    AppStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    Guid? PreviousVersionId,
    string? AmendmentReason,
    DateTimeOffset? AmendedAt,
    string? AmendedById,
    string? ConsolidatedById,
    DateTimeOffset? ConsolidatedOn,
    string? ApprovedById,
    DateTimeOffset? ApprovedOn,
    decimal TotalEstimatedBudget,
    IReadOnlyList<AppItemDto> Items,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record AnnualProcurementPlanSummaryDto(
    Guid Id,
    string AppNumber,
    int FiscalYear,
    AppRevisionType RevisionType,
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
    AppRevisionType RevisionType) : ICommand<AnnualProcurementPlanDto>;

public sealed record ConsolidatePpmpsCommand(
    Guid AppId,
    IReadOnlyList<Guid> PpmpIds) : ICommand<AnnualProcurementPlanDto>;

public sealed record PublishAnnualProcurementPlanCommand(Guid Id) : ICommand<AnnualProcurementPlanDto>;

public sealed record AmendAnnualProcurementPlanCommand(
    Guid Id,
    string AmendmentReason,
    AppRevisionType RevisionType) : ICommand<AnnualProcurementPlanDto>;

// ── Queries ───────────────────────────────────────────────────────────────────

public sealed record GetAnnualProcurementPlanQuery(Guid Id) : IQuery<AnnualProcurementPlanDto>;

public sealed record GetAppVersionsQuery(Guid VersionChainId) : IQuery<IReadOnlyList<AnnualProcurementPlanSummaryDto>>;

public sealed record SearchAnnualProcurementPlansQuery : IQuery<PagedResponse<AnnualProcurementPlanSummaryDto>>
{
    public string? Keyword { get; init; }
    public int? FiscalYear { get; init; }
    public AppStatus? Status { get; init; }
    public AppRevisionType? RevisionType { get; init; }
    public bool CurrentVersionOnly { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
