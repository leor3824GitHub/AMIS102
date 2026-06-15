using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintAccountability;

/// <summary>Renders an accountability document (ICS for SE, PAR for PPE) as a PDF.</summary>
public sealed record PrintAccountabilityQuery(
    Guid AccountabilityId,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 15d) : IQuery<byte[]>;
