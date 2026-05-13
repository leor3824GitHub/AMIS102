using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Incidents;

public sealed record PropertyIncidentItemDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    AssetSnapshotDto Snapshot,
    decimal SnapshotAcquisitionCost,
    decimal SnapshotCurrentReplacementCost,
    Guid? AccountabilityLineId,
    IncidentItemResolution ItemResolution,
    DateOnly? ResolvedOn);

public sealed record PropertyIncidentReportDto(
    Guid Id,
    string IncidentNo,
    PropertyIncidentType IncidentType,
    DateOnly IncidentDate,
    string FundCluster,
    string DepartmentOffice,
    string Circumstances,
    EmployeeRefDto AccountableOfficer,
    string AccountableOfficerDesignation,
    string? AccountableOfficerGovIdType,
    string? AccountableOfficerGovIdNo,
    DateOnly? AccountableOfficerGovIdIssuedOn,
    EmployeeRefDto? NotedBy,
    bool PoliceNotified,
    string? PoliceStation,
    DateOnly? PoliceNotifiedOn,
    string? PoliceBlotterRef,
    DateOnly? NotarizedOn,
    string? NotaryDocNo,
    string? NotaryPageNo,
    string? NotaryBookNo,
    string? NotarySeriesOf,
    PropertyIncidentStatus Status,
    DateOnly? ReliefRequestedOn,
    DateOnly? ReliefGrantedOn,
    string? ReliefGrantedRef,
    decimal? AmountSettled,
    DateOnly? SettledOn,
    DateOnly? RecoveredOn,
    IReadOnlyCollection<PropertyIncidentItemDto> Items);

public sealed record PropertyIncidentReportSummaryDto(
    Guid Id,
    string IncidentNo,
    PropertyIncidentType IncidentType,
    PropertyIncidentStatus Status,
    DateOnly IncidentDate,
    int ItemCount);

// ── Commands ───────────────────────────────────────────────────────────────

public sealed record FileIncidentItemRequest(Guid AssetRegistryId, Guid? AccountabilityLineId);

public sealed record FileIncidentReportCommand(
    PropertyIncidentType IncidentType,
    DateOnly IncidentDate,
    string FundCluster,
    string DepartmentOffice,
    string Circumstances,
    EmployeeRefDto AccountableOfficer,
    string AccountableOfficerDesignation,
    IReadOnlyList<FileIncidentItemRequest> Items) : ICommand<PropertyIncidentReportDto>;

public sealed record NotifyIncidentPoliceCommand(
    Guid IncidentReportId,
    string Station,
    DateOnly NotifiedOn,
    string BlotterRef) : ICommand<PropertyIncidentReportDto>;

public sealed record NotarizeIncidentReportCommand(
    Guid IncidentReportId,
    DateOnly NotarizedOn,
    string DocNo,
    string PageNo,
    string BookNo,
    string SeriesOf) : ICommand<PropertyIncidentReportDto>;

public sealed record RecordIncidentRecoveryCommand(
    Guid IncidentReportId,
    Guid ItemId,
    DateOnly RecoveredOn) : ICommand<PropertyIncidentReportDto>;

public sealed record RecordIncidentSettlementCommand(
    Guid IncidentReportId,
    Guid ItemId,
    decimal Amount,
    DateOnly SettledOn) : ICommand<PropertyIncidentReportDto>;

public sealed record GrantIncidentReliefCommand(
    Guid IncidentReportId,
    Guid ItemId,
    DateOnly GrantedOn,
    string DecisionRef) : ICommand<PropertyIncidentReportDto>;

public sealed record DerecognizeIncidentItemCommand(
    Guid IncidentReportId,
    Guid ItemId,
    DateOnly RecordedOn) : ICommand<PropertyIncidentReportDto>;

public sealed record CloseIncidentReportCommand(Guid IncidentReportId) : ICommand<PropertyIncidentReportDto>;

// ── Queries ────────────────────────────────────────────────────────────────

public sealed record GetIncidentReportQuery(Guid Id) : IQuery<PropertyIncidentReportDto?>;

public sealed record SearchIncidentReportsQuery(
    string? Keyword = null,
    PropertyIncidentType? IncidentType = null,
    PropertyIncidentStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<PropertyIncidentReportSummaryDto>>;

