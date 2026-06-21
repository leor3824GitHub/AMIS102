using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintParStickers;

/// <summary>
/// Renders a printable property sticker for every line on a Property Acknowledgement Receipt (PAR),
/// laid out 10 per sheet (2 × 5), each with a Property-No QR code. Rejects non-PAR documents.
/// </summary>
public sealed record PrintParStickersQuery(
    Guid   AccountabilityId,
    string PaperSize = "longbond") : IQuery<byte[]>;
