using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyIncidentReports.CreatePropertyIncidentReport;

/// <summary>
/// Creates a Report of Lost/Stolen/Damaged/Destroyed Semi-Expendable Property (RLSDDSP).
/// All listed properties are set to PropertyStatus.LostStolenDamaged.
/// If properties were Issued, the ICS is NOT cancelled — the accountable employee
/// remains liable until formally relieved through administrative / legal process.
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
    IReadOnlyList<Guid> PropertyIds) : ICommand<CreatePropertyIncidentReportResult>;

public sealed record CreatePropertyIncidentReportResult(Guid ReportId, string ReportNo, int ItemCount);
