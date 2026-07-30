using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Unserviceable;

public sealed record UnserviceablePropertyItemDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    AssetSnapshotDto Snapshot,
    DateOnly SnapshotDateAcquired,
    decimal SnapshotAcquisitionCost,
    decimal SnapshotAccumulatedDepreciation,
    decimal SnapshotAccumulatedImpairmentLosses,
    decimal SnapshotCarryingAmount,
    string? Remarks,
    DisposalMethod? DisposalMethod,
    string? DisposalOtherSpecify,
    decimal? AppraisedValue,
    DateOnly? DisposalRecordedOn,
    string? SaleORNo,
    decimal? SaleAmount);

public sealed record UnserviceablePropertyReportDto(
    Guid Id,
    string ReportNo,
    UnserviceableReportType ReportType,
    DateOnly AsAt,
    string FundCluster,
    string Station,
    UnserviceableReportStatus Status,
    EmployeeRefDto AccountableOfficer,
    EmployeeRefDto? ApprovedBy,
    EmployeeRefDto? InspectedBy,
    DateOnly? InspectedOn,
    EmployeeRefDto? WitnessedBy,
    DateOnly? WitnessedOn,
    IReadOnlyCollection<UnserviceablePropertyItemDto> Items);

public sealed record UnserviceablePropertyReportSummaryDto(
    Guid Id,
    string ReportNo,
    UnserviceableReportType ReportType,
    UnserviceableReportStatus Status,
    DateOnly AsAt,
    int ItemCount,
    bool HasSignedCopy = false);

// ── Commands ───────────────────────────────────────────────────────────────

// FundCluster is intentionally absent: it is derived from the first asset added (all items on one report
// share a cluster) rather than typed, so it can't disagree with the assets being disposed.
public sealed record CreateUnserviceableReportDraftCommand(
    UnserviceableReportType ReportType,
    string Station,
    DateOnly AsAt,
    EmployeeRefDto AccountableOfficer) : ICommand<UnserviceablePropertyReportDto>;

public sealed record AddUnserviceableReportItemCommand(
    Guid ReportId,
    Guid AssetRegistryId,
    string? Remarks) : ICommand<UnserviceablePropertyReportDto>;

/// <summary>Edits a Draft report's header fields. ReportType and FundCluster are immutable here —
/// the cluster is derived from the report's items, not hand-edited.</summary>
public sealed record UpdateUnserviceableReportHeaderCommand(
    Guid ReportId,
    string Station,
    DateOnly AsAt,
    EmployeeRefDto AccountableOfficer) : ICommand<UnserviceablePropertyReportDto>;

public sealed record SubmitUnserviceableReportCommand(
    Guid ReportId,
    EmployeeRefDto ApprovedBy) : ICommand<UnserviceablePropertyReportDto>;

public sealed record InspectionDecisionRequest(
    Guid ItemId,
    DisposalMethod Method,
    string? OtherSpecify,
    decimal? AppraisedValue);

public sealed record RecordUnserviceableInspectionCommand(
    Guid ReportId,
    EmployeeRefDto InspectedBy,
    DateOnly InspectedOn,
    EmployeeRefDto? WitnessedBy,
    DateOnly? WitnessedOn,
    IReadOnlyList<InspectionDecisionRequest> Decisions) : ICommand<UnserviceablePropertyReportDto>;

public sealed record DisposalRecordRequest(
    Guid ItemId,
    DateOnly DisposalRecordedOn,
    string? SaleORNo,
    decimal? SaleAmount);

public sealed record RecordUnserviceableDisposalCommand(
    Guid ReportId,
    IReadOnlyList<DisposalRecordRequest> Records) : ICommand<UnserviceablePropertyReportDto>;

public sealed record CloseUnserviceableReportCommand(Guid ReportId) : ICommand<UnserviceablePropertyReportDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetUnserviceableReportQuery(Guid Id) : IQuery<UnserviceablePropertyReportDto?>;

public sealed record SearchUnserviceableReportsQuery(
    string? Keyword = null,
    UnserviceableReportType? ReportType = null,
    UnserviceableReportStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<UnserviceablePropertyReportSummaryDto>>;

