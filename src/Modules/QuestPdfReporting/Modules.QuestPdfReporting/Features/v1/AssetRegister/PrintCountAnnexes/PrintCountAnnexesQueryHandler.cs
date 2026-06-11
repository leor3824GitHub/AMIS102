using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;

public sealed class PrintCountAnnexesQueryHandler(IMediator mediator)
    : IQueryHandler<PrintCountAnnexesQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintCountAnnexesQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetReconciliationReportQuery(query.SessionId), ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{query.SessionId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("PhysicalCount"), ct).ConfigureAwait(false);

        return new CountAnnexPdfDocument(
            report, org, signatories, query.Annex,
            query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
