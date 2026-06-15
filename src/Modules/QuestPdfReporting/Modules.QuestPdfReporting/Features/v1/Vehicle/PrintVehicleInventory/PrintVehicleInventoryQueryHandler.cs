using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;

public sealed class PrintVehicleInventoryQueryHandler(IMediator mediator)
    : IQueryHandler<PrintVehicleInventoryQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintVehicleInventoryQuery query, CancellationToken cancellationToken)
    {
        var inventory = await mediator.Send(
            new GetMotorVehicleInventoryQuery { Status = query.Status }, cancellationToken).ConfigureAwait(false);

        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var signatories = await mediator.Send(
            new GetReportSignatoriesQuery("VehicleInventory"), cancellationToken).ConfigureAwait(false);

        return new VehicleInventoryPdfDocument(
            inventory, org, signatories, query.AsOfDate, query.PaperSize, query.Orientation, (float)query.Margin).GeneratePdf();
    }
}
