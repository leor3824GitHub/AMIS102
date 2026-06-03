using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.InspectionAcceptanceReports.PrintInspectionAcceptanceReportFast;

public sealed record PrintInspectionAcceptanceReportFastQuery(
    Guid Id,
    string PaperSize = "a4",
    string Orientation = "landscape",
    int MinRows = 12) : IQuery<ReportFileDto>;
