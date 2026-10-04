using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintPpmp;

public sealed class PrintPpmpQueryHandler(IMediator mediator)
    : IQueryHandler<PrintPpmpQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintPpmpQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ppmp = await mediator.Send(new GetPpmpQuery(query.PpmpId), cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        // "Prepared by" is the PPMP's own preparer (End-User staff); null when the employee record is gone.
        var preparer = ppmp.PreparedById == Guid.Empty
            ? null
            : await mediator.Send(new GetEmployeeReferenceByIdQuery(ppmp.PreparedById), cancellationToken).ConfigureAwait(false);

        return new PpmpPdfDocument(
            ppmp, org, preparer, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
