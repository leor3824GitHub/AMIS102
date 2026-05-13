using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportById;

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
    Guid TangibleInventoryItemId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string AssetTypeAtTimeOfReport);

