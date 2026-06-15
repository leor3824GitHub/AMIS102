using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard;

public sealed class PrintPropertyCardQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPropertyCardQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPropertyCardQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var card = await mediator.Send(new GetPropertyCardQuery(query.PropertyNo), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{query.PropertyNo}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        return new PropertyCardPdfDocument(
            card, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
