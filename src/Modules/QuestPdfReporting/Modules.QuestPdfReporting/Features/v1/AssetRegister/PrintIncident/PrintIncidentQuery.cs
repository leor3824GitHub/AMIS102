using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIncident;

/// <summary>Renders the Report of Lost, Stolen, Damaged or Destroyed Property (RLSDDSP).</summary>
public sealed record PrintIncidentQuery(
    Guid IncidentReportId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 14d) : IQuery<byte[]>;
