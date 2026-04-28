using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

// ── Shared Enums ─────────────────────────────────────────────────────────────

public enum PpmpType
{
    Indicative = 0,
    Final = 1
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

public enum ModeOfProcurement
{
    PublicBidding = 0,
    LimitedSourceBidding = 1,
    DirectContracting = 2,
    RepeatOrder = 3,
    Shopping = 4,
    NegotiatedProcurement = 5
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record PpmpItemDto(
    Guid Id,
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
    string? SupportingDocuments,
    string? Remarks);

public sealed record PpmpDto(
    Guid Id,
    string PpmpNumber,
    int FiscalYear,
    PpmpType PpmpType,
    string OfficeCode,
    string EndUserUnit,
    PpmpStatus Status,
    int VersionNumber,
    bool IsCurrentVersion,
    Guid VersionChainId,
    Guid? PreviousVersionId,
    string? AmendmentReason,
    DateTimeOffset? AmendedAt,
    string? AmendedById,
    Guid PreparedById,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedById,
    string? ReturnReason,
    DateTimeOffset? ReturnedAt,
    Guid? ReturnedById,
    Guid? AppId,
    decimal TotalEstimatedBudget,
    IReadOnlyList<PpmpItemDto> Items,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc);

public sealed record PpmpSummaryDto(
    Guid Id,
    string PpmpNumber,
    int FiscalYear,
    PpmpType PpmpType,
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
    ModeOfProcurement ModeOfProcurement,
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
    PpmpType PpmpType,
    string OfficeCode,
    string EndUserUnit,
    Guid PreparedById,
    IReadOnlyList<PpmpItemRequest> Items) : ICommand<PpmpDto>;

public sealed record UpdatePpmpCommand(
    Guid Id,
    int FiscalYear,
    PpmpType PpmpType,
    string OfficeCode,
    string EndUserUnit,
    Guid PreparedById,
    IReadOnlyList<PpmpItemRequest> Items) : ICommand<PpmpDto>;

public sealed record SubmitPpmpCommand(Guid Id) : ICommand<PpmpDto>;

public sealed record ApprovePpmpCommand(Guid Id, Guid ApprovedById) : ICommand<PpmpDto>;

public sealed record RecallPpmpCommand(Guid Id) : ICommand<PpmpDto>;

public sealed record ReturnPpmpCommand(Guid Id, string ReturnReason, Guid ReturnedById) : ICommand<PpmpDto>;

public sealed record AmendPpmpCommand(
    Guid Id,
    string AmendmentReason) : ICommand<PpmpDto>;

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
    public PpmpType? PpmpType { get; init; }
    public bool CurrentVersionOnly { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
