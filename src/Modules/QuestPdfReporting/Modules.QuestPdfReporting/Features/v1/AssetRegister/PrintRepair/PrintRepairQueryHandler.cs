using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRepair;

public sealed class PrintRepairQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRepairQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintRepairQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var repair = await mediator.Send(new GetPropertyRepairQuery(query.RepairId), cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Repair (RPRI) '{query.RepairId}' not found.");

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        return new RepairPdfDocument(repair, org, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
