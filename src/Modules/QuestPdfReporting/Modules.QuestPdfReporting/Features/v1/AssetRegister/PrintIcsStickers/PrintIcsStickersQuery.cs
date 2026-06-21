using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIcsStickers;

/// <summary>
/// Renders a printable property sticker for every line on an Inventory Custodian Slip (ICS),
/// laid out 10 per sheet (2 × 5), each with a Property-No QR code. Rejects non-ICS documents.
/// </summary>
public sealed record PrintIcsStickersQuery(
    Guid   AccountabilityId,
    string PaperSize = "longbond") : IQuery<byte[]>;
