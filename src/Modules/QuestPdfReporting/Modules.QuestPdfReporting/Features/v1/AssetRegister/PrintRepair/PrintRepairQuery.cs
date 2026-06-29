using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRepair;

/// <summary>
/// Renders a Request for Pre/Post Repair Inspection (RPRI — NFA Exhibit 6) as a PDF.
/// </summary>
public sealed record PrintRepairQuery(
    Guid RepairId,
    string PaperSize = "longbond",
    string Orientation = "portrait",
    double Margin = 14d) : IQuery<byte[]>;
