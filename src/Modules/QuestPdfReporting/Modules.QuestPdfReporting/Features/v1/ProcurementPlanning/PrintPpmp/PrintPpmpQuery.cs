using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintPpmp;

/// <summary>
/// Renders a PPMP as a PDF in the GPPB PPMP form layout: letterhead, PPMP No., Indicative / Final boxes,
/// Fiscal Year + End-User header, the 12-column project table, Total Budget, and Prepared by / Submitted by.
/// </summary>
public sealed record PrintPpmpQuery(
    Guid PpmpId,
    string PaperSize = "longbond", // 8.5 × 13 in
    string Orientation = "landscape",
    double Margin = 8d) : IQuery<byte[]>;
