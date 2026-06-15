using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPhysicalCountReport;

public sealed class PrintPhysicalCountReportQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPhysicalCountReportQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPhysicalCountReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetPhysicalCountReportQuery(query.SessionId, query.AssetType), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{query.SessionId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("PhysicalCount"), cancellationToken).ConfigureAwait(false);

        return new PhysicalCountReportPdfDocument(
            report, query.AssetType, org, signatories,
            query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
