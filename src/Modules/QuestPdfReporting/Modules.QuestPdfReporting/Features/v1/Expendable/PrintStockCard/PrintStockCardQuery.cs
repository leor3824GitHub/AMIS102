using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;

public sealed record PrintStockCardQuery(Guid ProductId) : IQuery<byte[]>;
