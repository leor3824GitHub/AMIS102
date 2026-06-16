using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintReturnedProperty;

public sealed class PrintReturnedPropertyQueryHandler(IMediator mediator)
    : IQueryHandler<PrintReturnedPropertyQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintReturnedPropertyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var receipt = await mediator.Send(new GetReturnedPropertyReceiptQuery(query.ReceiptId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Returned-property receipt '{query.ReceiptId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new ReturnedPropertyPdfDocument(
            receipt, org, query.PaperSize, query.Orientation, (float)query.Margin, query.MinRows).GeneratePdf();
    }
}
