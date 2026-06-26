using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;

public sealed class PrintJobOrderQueryHandler(IMediator mediator)
    : IQueryHandler<PrintJobOrderQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintJobOrderQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var jo = await mediator.Send(new GetJobOrderQuery(query.JobOrderId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Job order '{query.JobOrderId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new JobOrderPdfDocument(
            jo, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
