using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;

internal static class PrintVehicleInventoryEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/inventory/pdf", Print)
            .WithName("QuestPdfReporting_PrintVehicleInventory")
            .WithSummary("Generate a QuestPDF for the Motor Vehicle Inventory report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewVehicleReports);

    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        string?   status    = null,
        DateTime? asOfDate  = null)
    {
        var bytes = await mediator.Send(new PrintVehicleInventoryQuery(status, asOfDate), ct);
        return TypedResults.File(bytes, "application/pdf", "VehicleInventory.pdf");
    }
}
