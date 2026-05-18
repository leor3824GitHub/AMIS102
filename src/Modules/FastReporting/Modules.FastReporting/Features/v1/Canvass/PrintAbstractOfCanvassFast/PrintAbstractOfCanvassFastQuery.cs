using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Canvass.PrintAbstractOfCanvassFast;

public sealed record PrintAbstractOfCanvassFastQuery(
    Guid Id,
    string PaperSize = "a4",
    string Orientation = "portrait",
    int MinRows = 8) : IQuery<ReportFileDto>;
