using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

/// <summary>Renders a single asset's printable property sticker (with a Property-No QR code) as a PDF.</summary>
public sealed record PrintPropertyStickerQuery(
    string PropertyNo,
    string PaperSize = "longbond") : IQuery<byte[]>;
