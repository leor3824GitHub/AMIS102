using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;

public sealed class PrintAppQueryHandler(IMediator mediator)
    : IQueryHandler<PrintAppQuery, byte[]>
{
    internal const string SignatoryReportType = "AnnualProcurementPlan";

    public async ValueTask<byte[]> Handle(PrintAppQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var app = await mediator.Send(new GetAnnualProcurementPlanQuery(query.AppId), cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        // The master-data query returns inactive rows too — only active signatories are printed.
        var signatories = (await mediator.Send(
                new GetReportSignatoriesQuery(SignatoryReportType), cancellationToken).ConfigureAwait(false))
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToList();

        return new AppPdfDocument(
            app, org, signatories, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
