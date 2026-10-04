using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

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

/// <summary>Grouping of a line item on the GPPB APP form (RA 12009).</summary>
public enum AppSection
{
    /// <summary>General Requirements — projects procured through the regular modes.</summary>
    GeneralRequirements = 0,

    /// <summary>Miscellaneous Items (for Direct Acquisition only), Sec 32.2 of RA No. 12009.</summary>
    DirectAcquisition = 1,

    /// <summary>Common-Use Supplies and Equipment (CSE) to be purchased from PS-DBM.</summary>
    CommonUseSupplies = 2
}

/// <summary>Criteria for bid evaluation printed in Column 6 of the APP form.</summary>
public enum BidEvaluationCriteria
{
    NotApplicable = 0,

    /// <summary>Lowest Calculated Responsive Bid.</summary>
    Lcrb = 1,

    /// <summary>Most Economically Advantageous Responsive Bid (RA 12009).</summary>
    Mearb = 2,

    /// <summary>Highest Rated Responsive Bid (consulting services).</summary>
    Hrrb = 3
}

/// <summary>Defaults shared by the API and the UI so a PPMP item is pre-classified the same way everywhere.</summary>
public static class ProcurementPlanRules
{
    public static bool IsCompetitiveBidding(string? modeOfProcurement) =>
        !string.IsNullOrWhiteSpace(modeOfProcurement) &&
        (modeOfProcurement.Contains("Competitive Bidding", StringComparison.OrdinalIgnoreCase) ||
         modeOfProcurement.Contains("Public Bidding", StringComparison.OrdinalIgnoreCase));

    public static AppSection SuggestSection(string? modeOfProcurement)
    {
        if (string.IsNullOrWhiteSpace(modeOfProcurement))
            return AppSection.GeneralRequirements;
        if (modeOfProcurement.Contains("Direct Acquisition", StringComparison.OrdinalIgnoreCase))
            return AppSection.DirectAcquisition;
        if (modeOfProcurement.Contains("PS-DBM", StringComparison.OrdinalIgnoreCase) ||
            modeOfProcurement.Contains("Procurement Service", StringComparison.OrdinalIgnoreCase))
            return AppSection.CommonUseSupplies;
        return AppSection.GeneralRequirements;
    }

    public static BidEvaluationCriteria SuggestCriteria(string? modeOfProcurement, ProjectType projectType)
    {
        if (!IsCompetitiveBidding(modeOfProcurement))
            return BidEvaluationCriteria.NotApplicable;
        return projectType == ProjectType.ConsultingServices ? BidEvaluationCriteria.Hrrb : BidEvaluationCriteria.Lcrb;
    }

    public static string ToDisplay(this BidEvaluationCriteria criteria) => criteria switch
    {
        BidEvaluationCriteria.Lcrb => "LCRB",
        BidEvaluationCriteria.Mearb => "MEARB",
        BidEvaluationCriteria.Hrrb => "HRRB",
        _ => "N/A"
    };

    /// <summary>Section heading row exactly as printed on the GPPB APP form.</summary>
    public static string ToFormTitle(this AppSection section) => section switch
    {
        AppSection.DirectAcquisition => "Miscellaneous Items (for Direct Acquisition only) Sec 32.2 of RA No. 12009",
        AppSection.CommonUseSupplies => "Common Use Supplies and Equipment (CSE) to be purchased from PS-DBM (kindly indicate the summary/total amounts only)",
        _ => "General Requirements"
    };

    public static string ToDisplay(this AppSection section) => section switch
    {
        AppSection.DirectAcquisition => "Direct Acquisition",
        AppSection.CommonUseSupplies => "CSE (PS-DBM)",
        _ => "General"
    };
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
    string? Remarks,
    string? FundingSourceCode = null,
    string? ProjectTitle = null,
    AppSection Section = AppSection.GeneralRequirements,
    bool IsEarlyProcurement = false,
    BidEvaluationCriteria BidEvaluationCriteria = BidEvaluationCriteria.NotApplicable,
    string? ProcurementStrategy = null);

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
    string? Remarks,
    string? FundingSourceCode = null,
    string? ProjectTitle = null,
    AppSection Section = AppSection.GeneralRequirements,
    bool IsEarlyProcurement = false,
    BidEvaluationCriteria BidEvaluationCriteria = BidEvaluationCriteria.NotApplicable,
    string? ProcurementStrategy = null);

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

public sealed record DeletePpmpCommand(Guid Id) : ICommand<Unit>;

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

