using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Issuance;

public sealed record PropertyIssuanceReportLineDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    int ItemNo,
    AssetSnapshotDto Snapshot,
    decimal SnapshotUnitCost,
    decimal SnapshotAmount,
    decimal? AccumulatedDepreciation,
    decimal? BookValue);

public sealed record PropertyIssuanceReportDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly Date,
    IssuanceNature Nature,
    EmployeeRefDto IssuedBy,
    EmployeeRefDto ApprovedBy,
    EmployeeRefDto IssuedTo,
    string IssuedToOfficeAddress,
    EmployeeRefDto ReceivedBy,
    DateOnly? DateReceived,
    string? DriverName,
    string? BillOfLadingNo,
    string? Remarks,
    IReadOnlyCollection<PropertyIssuanceReportLineDto> Lines);

public sealed record PropertyIssuanceReportSummaryDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceNature Nature,
    DateOnly Date,
    int LineCount,
    decimal TotalAmount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record CreateIssuanceReportCommand(
    IssuanceReportType ReportType,
    DateOnly Date,
    string FundCluster,
    IssuanceNature Nature,
    EmployeeRefDto IssuedBy,
    EmployeeRefDto ApprovedBy,
    EmployeeRefDto IssuedTo,
    string IssuedToOfficeAddress,
    EmployeeRefDto ReceivedBy,
    DateOnly? DateReceived,
    string? DriverName,
    string? BillOfLadingNo,
    string? Remarks,
    IReadOnlyList<Guid> AssetRegistryIds) : ICommand<PropertyIssuanceReportDto>;

public sealed record LineDepreciationDto(
    Guid LineId,
    decimal AccumulatedDepreciation,
    decimal BookValue);

public sealed record UpdateIssuanceReportDepreciationCommand(
    Guid ReportId,
    IReadOnlyList<LineDepreciationDto> Lines) : ICommand<PropertyIssuanceReportDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetIssuanceReportQuery(Guid Id) : IQuery<PropertyIssuanceReportDto?>;

public sealed record SearchIssuanceReportsQuery(
    string? Keyword = null,
    IssuanceReportType? ReportType = null,
    IssuanceNature? Nature = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyIssuanceReportSummaryDto>>;
