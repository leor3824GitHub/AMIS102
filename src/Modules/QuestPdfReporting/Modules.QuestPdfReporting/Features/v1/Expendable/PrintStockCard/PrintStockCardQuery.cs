using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;

public sealed record PrintStockCardQuery(
    Guid   ProductId,
    string PaperSize   = "a4",
    string Orientation = "landscape") : IQuery<byte[]>;
