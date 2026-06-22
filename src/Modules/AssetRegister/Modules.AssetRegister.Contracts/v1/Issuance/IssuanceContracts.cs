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
    string? Remarks,
    IReadOnlyCollection<PropertyIssuanceReportLineDto> Lines);

public sealed record PropertyIssuanceReportSummaryDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceNature Nature,
    DateOnly Date,
    int LineCount,
    decimal TotalAmount,
    bool HasSignedCopy = false);

// ── Commands ───────────────────────────────────────────────────────────────

// ApprovedBy is intentionally not part of the request: the approving authority is the
// organization's approving official, resolved server-side from the Organization Profile
// and snapshotted onto the report at creation time.
public sealed record CreateIssuanceReportCommand(
    IssuanceReportType ReportType,
    DateOnly Date,
    string FundCluster,
    IssuanceNature Nature,
    EmployeeRefDto IssuedBy,
    EmployeeRefDto IssuedTo,
    string IssuedToOfficeAddress,
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

// ── PPEIR Form Series ──────────────────────────────────────────────────────

public sealed record PPEIRFormSeriesDto(
    Guid Id,
    string Label,
    int StartSerial,
    int EndSerial,
    int NextSerial,
    int Remaining,
    bool IsActive,
    bool IsExhausted,
    bool IsUnused);

public sealed record CreatePPEIRFormSeriesCommand(
    string Label,
    int StartSerial,
    int EndSerial) : ICommand<PPEIRFormSeriesDto>;

public sealed record UpdatePPEIRFormSeriesCommand(
    Guid Id,
    string Label,
    int StartSerial,
    int EndSerial) : ICommand<PPEIRFormSeriesDto>;

public sealed record DeletePPEIRFormSeriesCommand(Guid Id) : ICommand<Unit>;

public sealed record ActivatePPEIRFormSeriesCommand(Guid Id) : ICommand<PPEIRFormSeriesDto>;

public sealed record DeactivatePPEIRFormSeriesCommand(Guid Id) : ICommand<PPEIRFormSeriesDto>;

public sealed record GetActivePPEIRFormSeriesQuery : IQuery<PPEIRFormSeriesDto?>;

public sealed record SearchPPEIRFormSeriesQuery(
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<PPEIRFormSeriesDto>>;
