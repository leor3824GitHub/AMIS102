using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIncident;

public sealed class PrintIncidentQueryHandler(IMediator mediator)
    : IQueryHandler<PrintIncidentQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintIncidentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetIncidentReportDocumentQuery(query.IncidentReportId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{query.IncidentReportId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new IncidentPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
