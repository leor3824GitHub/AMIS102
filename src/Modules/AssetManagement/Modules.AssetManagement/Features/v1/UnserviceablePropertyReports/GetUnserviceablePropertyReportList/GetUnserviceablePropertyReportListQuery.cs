using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportList;

public sealed record GetUnserviceablePropertyReportListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    DisposalMethod? DisposalMethod,
    Guid? InspectedByEmployeeId,
    int PageNumber = 1,
    int PageSize   = 10) : IQuery<PagedUnserviceablePropertyReportListResponse>;

public sealed record PagedUnserviceablePropertyReportListResponse(
    IReadOnlyList<UnserviceablePropertyReportSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record UnserviceablePropertyReportSummaryDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string DisposalMethod,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);

