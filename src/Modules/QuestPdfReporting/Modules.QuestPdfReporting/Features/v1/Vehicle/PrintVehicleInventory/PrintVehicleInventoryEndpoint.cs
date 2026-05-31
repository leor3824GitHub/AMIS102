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

    // ?pageWidth=a4|legal|longbond|letter   (default a4)
    // ?orientation=landscape|portrait        (default landscape)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        string?   status      = null,
        DateTime? asOfDate    = null,
        string?   pageWidth   = null,
        string?   orientation = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";

        var bytes = await mediator.Send(
            new PrintVehicleInventoryQuery(status, asOfDate, paperSize, orient), ct);
        return TypedResults.File(bytes, "application/pdf", "VehicleInventory.pdf");
    }
}
