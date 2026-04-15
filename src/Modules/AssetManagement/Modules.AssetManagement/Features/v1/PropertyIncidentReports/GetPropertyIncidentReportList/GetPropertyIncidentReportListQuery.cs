using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportList;

public sealed record GetPropertyIncidentReportListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PropertyIncidentType? IncidentType,
    Guid? AccountableEmployeeId,
    int PageNumber = 1,
    int PageSize   = 10) : IQuery<PagedPropertyIncidentReportListResponse>;

public sealed record PagedPropertyIncidentReportListResponse(
    IReadOnlyList<PropertyIncidentReportSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PropertyIncidentReportSummaryDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    string IncidentType,
    Guid? AccountableEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);
