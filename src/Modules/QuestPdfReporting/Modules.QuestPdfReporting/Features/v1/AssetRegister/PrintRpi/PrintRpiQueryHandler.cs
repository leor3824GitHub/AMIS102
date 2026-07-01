using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRpi;

public sealed class PrintRpiQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRpiQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintRpiQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // int.MaxValue page size = "all rows" — the paginated data query is reused as-is for the full print.
        var report = await mediator.Send(
            new GetRpiReportQuery(query.DateFrom, query.DateTo, 1, int.MaxValue),
            cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new RpiPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
