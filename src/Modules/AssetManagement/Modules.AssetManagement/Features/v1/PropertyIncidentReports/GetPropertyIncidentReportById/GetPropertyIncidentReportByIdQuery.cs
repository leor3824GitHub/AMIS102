using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportById;

public sealed record GetPropertyIncidentReportByIdQuery(Guid Id) : IQuery<PropertyIncidentReportDetailsDto>;

public sealed record PropertyIncidentReportDetailsDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    string IncidentType,
    string? FundCluster,
    Guid? AccountableEmployeeId,
    string IncidentDetails,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PropertyIncidentItemDetailsDto> Items);

public sealed record PropertyIncidentItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfReport);
