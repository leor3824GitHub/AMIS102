using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintInventoryCountForm;

public sealed class PrintInventoryCountFormQueryHandler(IMediator mediator)
    : IQueryHandler<PrintInventoryCountFormQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintInventoryCountFormQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var report = await mediator.Send(new GetReconciliationReportQuery(query.SessionId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Physical count session '{query.SessionId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new InventoryCountFormPdfDocument(
            report, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
