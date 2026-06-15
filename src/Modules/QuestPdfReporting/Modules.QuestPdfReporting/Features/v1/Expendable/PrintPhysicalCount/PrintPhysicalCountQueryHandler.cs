using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

public sealed class PrintPhysicalCountQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPhysicalCountQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPhysicalCountQuery query, CancellationToken cancellationToken)
    {
        var groups = await mediator.Send(
            new GetPhysicalCountReportQuery { WarehouseLocationId = query.WarehouseLocationId }, cancellationToken)
            .ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("PhysicalCount"), cancellationToken).ConfigureAwait(false);

        return new PhysicalCountPdfDocument(
            groups, org, signatories, query.AsOfDate, query.AssumedAccountabilityDate,
            query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
