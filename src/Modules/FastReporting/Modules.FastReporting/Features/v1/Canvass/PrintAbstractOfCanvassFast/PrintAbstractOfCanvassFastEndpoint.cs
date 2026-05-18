using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.Canvass.PrintAbstractOfCanvassFast;

public static class PrintAbstractOfCanvassFastEndpoint
{
    private const string CanvassRequestsView = "Permissions.Procurement.CanvassRequests.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintAbstractOfCanvass")
            .WithSummary("Generate a FastReport PDF for an Abstract of Canvass (1 AOC per page)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CanvassRequestsView);

    // Query: ?pageWidth=a4|legal|longbond|letter  (default a4)
    //        ?orientation=portrait|landscape      (default portrait)
    //        ?minRows=1..20                       (default 8)
    private static async Task<IResult> PrintFast(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape"
            ? "landscape"
            : "portrait";
        var rows = Math.Clamp(minRows ?? 8, 1, 20);

        var dto = await mediator.Send(
            new PrintAbstractOfCanvassFastQuery(id, paperSize, orient, rows), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
