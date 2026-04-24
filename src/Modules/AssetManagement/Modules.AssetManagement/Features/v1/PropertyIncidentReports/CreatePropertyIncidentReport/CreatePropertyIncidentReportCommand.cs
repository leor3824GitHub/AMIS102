using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

/// <summary>
/// Creates a Report of Lost/Stolen/Damaged/Destroyed Semi-Expendable Property (RLSDDSP).
/// The listed TangibleInventoryItems are snapshotted into PropertyIncidentItem rows.
/// The accountable employee remains liable until formally relieved through administrative
/// or legal process; this report does not automatically cancel the underlying ICS/PAR.
/// </summary>
public sealed record CreatePropertyIncidentReportCommand(
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    PropertyIncidentType IncidentType,
    string? FundCluster,
    Guid? AccountableEmployeeId,
    string IncidentDetails,
    string? Remarks,
    IReadOnlyList<Guid> TangibleInventoryItemIds) : ICommand<CreatePropertyIncidentReportResult>;

public sealed record CreatePropertyIncidentReportResult(Guid ReportId, string ReportNo, int ItemCount);
