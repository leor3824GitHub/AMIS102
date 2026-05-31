using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;

public sealed class PrintStockCardQueryHandler(IMediator mediator)
    : IQueryHandler<PrintStockCardQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintStockCardQuery query, CancellationToken ct)
    {
        var card = await mediator.Send(new GetStockCardQuery(query.ProductId), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"No stock card found for product {query.ProductId}.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        return new StockCardPdfDocument(card, org, query.PaperSize, query.Orientation).GeneratePdf();
    }
}
