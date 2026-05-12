using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace FSH.Modules.AssetRegister.Contracts.v1.Unserviceable;

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
    int ItemCount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreateUnserviceableReportDraftCommand(
    UnserviceableReportType ReportType,
    string FundCluster,
    string Station,
    DateOnly AsAt,
    EmployeeRefDto AccountableOfficer) : ICommand<UnserviceablePropertyReportDto>;

public sealed record AddUnserviceableReportItemCommand(
    Guid ReportId,
    Guid AssetRegistryId,
    string? Remarks) : ICommand<UnserviceablePropertyReportDto>;

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
