using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;

public sealed class PrintRegSpiQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRegSpiQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintRegSpiQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(
            new GetRegSpiReportQuery(query.AsOfDate, query.AssetType, query.CustodianId), cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new RegSpiPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
