using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegPpei;

public sealed class PrintRegPpeiQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRegPpeiQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintRegPpeiQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(
            new GetRegPpeiReportQuery(query.AsOfDate, query.CustodianId, query.FundCluster, query.PropertyClass), cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new RegPpeiPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
