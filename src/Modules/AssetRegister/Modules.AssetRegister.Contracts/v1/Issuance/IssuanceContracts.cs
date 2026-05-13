using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Issuance;

public sealed record PropertyIssuanceReportLineDto(
    Guid Id,
    Guid ReportId,
    Guid AccountabilityId,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    AssetSnapshotDto Snapshot,
    string? SnapshotResponsibilityCenterCode,
    int SnapshotQuantityIssued,
    decimal SnapshotUnitCost,
    decimal SnapshotAmount);

public sealed record PropertyIssuanceReportDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    IssuanceReportStatus Status,
    EmployeeRefDto PreparedBy,
    EmployeeRefDto? CertifiedBy,
    EmployeeRefDto? PostedBy,
    DateOnly? PostedOn,
    IReadOnlyCollection<PropertyIssuanceReportLineDto> Lines);

public sealed record PropertyIssuanceReportSummaryDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceReportStatus Status,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    int LineCount,
    decimal TotalAmount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreateIssuanceReportDraftCommand(
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    EmployeeRefDto PreparedBy) : ICommand<PropertyIssuanceReportDto>;

public sealed record AddIssuanceReportLinesCommand(
    Guid ReportId,
    IReadOnlyList<Guid> AccountabilityLineIds) : ICommand<PropertyIssuanceReportDto>;

public sealed record RemoveIssuanceReportLineCommand(Guid ReportId, Guid LineId) : ICommand<PropertyIssuanceReportDto>;

public sealed record PostIssuanceReportCommand(
    Guid ReportId,
    EmployeeRefDto CertifiedBy,
    EmployeeRefDto PostedBy,
    DateOnly PostedOn) : ICommand<PropertyIssuanceReportDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetIssuanceReportQuery(Guid Id) : IQuery<PropertyIssuanceReportDto?>;

public sealed record SearchIssuanceReportsQuery(
    string? Keyword = null,
    IssuanceReportType? ReportType = null,
    IssuanceReportStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyIssuanceReportSummaryDto>>;

