using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRpi;

/// <summary>Renders the Report on Property Issued (RPI) PDF — PPE issued via PAR.</summary>
public sealed record PrintRpiQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
