using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportById;

public sealed record GetUnserviceablePropertyReportByIdQuery(Guid Id) : IQuery<UnserviceablePropertyReportDetailsDto>;

public sealed record UnserviceablePropertyReportDetailsDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string DisposalMethod,
    string? FundCluster,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<UnserviceablePropertyItemDetailsDto> Items);

public sealed record UnserviceablePropertyItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfReport,
    string? ConditionRemarks);
