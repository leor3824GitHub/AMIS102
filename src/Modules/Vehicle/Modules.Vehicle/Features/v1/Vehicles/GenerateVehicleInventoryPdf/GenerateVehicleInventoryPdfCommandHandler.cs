using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using QuestPDF.Fluent;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.GenerateVehicleInventoryPdf;

public sealed class GenerateVehicleInventoryPdfCommandHandler(IMediator mediator)
    : ICommandHandler<GenerateVehicleInventoryPdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(
        GenerateVehicleInventoryPdfCommand command, CancellationToken cancellationToken)
    {
        // Avoid concurrent operations on MasterDataDbContext by executing MasterData queries sequentially.
        var inventory = await mediator.Send(
            new GetMotorVehicleInventoryQuery { Status = command.Status }, cancellationToken).ConfigureAwait(false);

        var organization = await mediator.Send(
            new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);

        var signatories = await mediator.Send(
            new GetReportSignatoriesQuery("VehicleInventory"), cancellationToken).ConfigureAwait(false);

        return new VehicleInventoryPdfDocument(
            inventory,
            organization,
            signatories,
            command.AsOfDate)
            .GeneratePdf();
    }
}

