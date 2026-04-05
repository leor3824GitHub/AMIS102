using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.GenerateVehicleInventoryPdf;

public static class GenerateVehicleInventoryPdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/inventory/pdf", Generate)
            .WithName(nameof(GenerateVehicleInventoryPdfCommand))
            .WithSummary("Generate a PDF for the Motor Vehicle Inventory report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.View);

    private static async Task<IResult> Generate(
        GenerateVehicleInventoryPdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "VehicleInventory.pdf");
    }
}
