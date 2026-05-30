using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.Vehicles.PrintVehicleInventoryFast;

public static class PrintVehicleInventoryFastEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/print", PrintFast)
            .WithName("FastReporting_PrintVehicleInventory")
            .WithSummary("Generate a FastReport PDF for the Inventory of Motor Vehicles")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .RequirePermission(VehiclePermissions.Vehicles.View);

    // Query: ?status=Active|UnderRepair|Retired|Decommissioned  (optional — all when omitted)
    //        ?asOfDate=2026-05-31                               (default today)
    //        ?pageWidth=a4|legal|longbond|letter                (default a4)
    //        ?orientation=landscape|portrait                    (default landscape)
    private static async Task<IResult> PrintFast(
        IMediator mediator,
        CancellationToken ct,
        string? status = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        string? orientation = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait"
            ? "portrait"
            : "landscape";

        var dto = await mediator.Send(
            new PrintVehicleInventoryFastQuery(status, asOfDate, paperSize, orient), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
