using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintInventoryCountForm;

/// <summary>
/// Renders the COA Inventory Count Form (Annex A) — the physical-count working paper that
/// lists every counted line with Quantity per Property Card vs Quantity per Physical Count,
/// drawn from a session's reconciliation data.
/// </summary>
public sealed record PrintInventoryCountFormQuery(
    Guid SessionId,
    string PaperSize = "a4",
    string Orientation = "landscape",
    double Margin = 12d) : IQuery<byte[]>;
