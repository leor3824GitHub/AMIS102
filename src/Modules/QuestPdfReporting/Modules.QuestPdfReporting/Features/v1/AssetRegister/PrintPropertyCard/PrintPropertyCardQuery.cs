using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard;

/// <summary>Renders an asset's Property Card (movement history with running balance) as a PDF.</summary>
public sealed record PrintPropertyCardQuery(
    string PropertyNo,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
