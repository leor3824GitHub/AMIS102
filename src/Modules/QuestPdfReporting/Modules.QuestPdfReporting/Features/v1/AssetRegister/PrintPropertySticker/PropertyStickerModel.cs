using AMIS.Modules.AssetRegister.Contracts.v1;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

/// <summary>
/// Render-ready data for a single property sticker. Sourced either from a single asset lookup
/// (reprint) or from one ICS/PAR line snapshot (bulk generation off an accountability document).
/// </summary>
internal sealed record PropertyStickerModel(
    string    PropertyNo,
    string    Description,
    string?   SerialNo,
    DateOnly  AcquisitionDate,
    decimal   UnitCost,
    AssetType AssetType,
    string?   AccountableOfficer,
    string?   Location);
