using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintUnserviceable;

public sealed class PrintUnserviceableQueryHandler(IMediator mediator)
    : IQueryHandler<PrintUnserviceableQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintUnserviceableQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetUnserviceableReportDocumentQuery(query.ReportId), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Unserviceable report '{query.ReportId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        return new UnserviceablePdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
