using AMIS.Modules.Expendable.Contracts.v1.Reports;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GenerateStockCardPdf;

public sealed class GenerateStockCardPdfCommandHandler(IMediator mediator)
    : ICommandHandler<GenerateStockCardPdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(
        GenerateStockCardPdfCommand command, CancellationToken cancellationToken)
    {
        var card = await mediator.Send(new GetStockCardQuery(command.ProductId), cancellationToken)
            .ConfigureAwait(false);
        if (card is null)
            throw new InvalidOperationException($"No stock card found for product {command.ProductId}.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken)
            .ConfigureAwait(false);

        return new StockCardPdfDocument(card, org).GeneratePdf();
    }
}

