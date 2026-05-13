using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

// ── Shared Enums ─────────────────────────────────────────────────────────────

public enum PpmpPhase
{
    Indicative = 0,
    Final = 1,
    Updated = 2
}

public enum PpmpStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Consolidated = 3,
    Superseded = 4,
    Returned = 5
}

public enum ProjectType
{
    Goods = 0,
    Infrastructure = 1,
    ConsultingServices = 2
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record PpmpItemDto(
    Guid Id,
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
    string? SupportingDocuments,
    string? Remarks);

public sealed record PpmpDto(
    Guid Id,
    string PpmpNumber,
    int FiscalYear,
    PpmpPhase Phase,
    string OfficeCode,
    string EndUserUnit,
    PpmpStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    Guid? PreviousVersionId,
    string? AmendmentReason,
    DateTimeOffset? AmendedAt,
    Guid? AmendedById,
    Guid PreparedById,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedById,
    string? ReturnReason,
    DateTimeOffset? ReturnedAt,
    Guid? ReturnedById,
    decimal TotalEstimatedBudget,
    IReadOnlyList<PpmpItemDto> Items,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record PpmpSummaryDto(
    Guid Id,
    string PpmpNumber,
    int FiscalYear,
    PpmpPhase Phase,
    string OfficeCode,
    string EndUserUnit,
    PpmpStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    int ItemCount,
    decimal TotalEstimatedBudget,
    DateTimeOffset CreatedOnUtc);

// ── Request Models ────────────────────────────────────────────────────────────

public sealed record PpmpItemRequest(
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
    string? SupportingDocuments,
    string? Remarks);

// ── Commands ─────────────────────────────────────────────────────────────────

public sealed record CreatePpmpCommand(
    int FiscalYear,
    PpmpPhase Phase,
    string OfficeCode,
    string EndUserUnit,
    Guid PreparedById,
    IReadOnlyList<PpmpItemRequest> Items) : ICommand<PpmpDto>;

public sealed record UpdatePpmpCommand(
    Guid Id,
    int FiscalYear,
    string OfficeCode,
    string EndUserUnit,
    Guid PreparedById,
    IReadOnlyList<PpmpItemRequest> Items) : ICommand<PpmpDto>;

public sealed record SubmitPpmpCommand(Guid Id) : ICommand<PpmpDto>;

public sealed record ApprovePpmpCommand(Guid Id) : ICommand<PpmpDto>;

public sealed record RecallPpmpCommand(Guid Id) : ICommand<PpmpDto>;

public sealed record ReturnPpmpCommand(Guid Id, string ReturnReason) : ICommand<PpmpDto>;

/// <summary>Promotes an Approved Indicative PPMP to a new Final draft (per-phase version reset to 1).</summary>
public sealed record PromoteToFinalPpmpCommand(Guid Id) : ICommand<PpmpDto>;

/// <summary>Creates a new Updated version of an Approved/Consolidated Final or Updated PPMP. Caller may add a reason.</summary>
public sealed record CreateUpdatePpmpCommand(Guid Id, string UpdateReason) : ICommand<PpmpDto>;

// ── Queries ───────────────────────────────────────────────────────────────────

public sealed record GetPpmpQuery(Guid Id) : IQuery<PpmpDto>;

public sealed record GetPpmpVersionsQuery(Guid VersionChainId) : IQuery<IReadOnlyList<PpmpSummaryDto>>;

public sealed record GetAvailablePpmpsForAppQuery(int FiscalYear, Guid? AppId = null) : IQuery<IReadOnlyList<PpmpSummaryDto>>;

public sealed record SearchPpmpsQuery : IQuery<PagedResponse<PpmpSummaryDto>>
{
    public string? Keyword { get; init; }
    public int? FiscalYear { get; init; }
    public string? OfficeCode { get; init; }
    public PpmpStatus? Status { get; init; }
    public PpmpPhase? Phase { get; init; }
    public bool CurrentVersionOnly { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
