using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.AssetManagement.PrintICSFast;

public static class PrintICSFastEndpoint
{
    private const string ICSView = "Permissions.AssetManagement.InventoryCustodianSlips.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintICS")
            .WithSummary("Generate a FastReport PDF for an Inventory Custodian Slip (ICS)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ICSView);

    // Query: ?pageWidth=longbond|a4|legal   (default longbond)
    //        ?orientation=landscape|portrait (default landscape)
    //        ?minRows=1..30                  (default 15)
    // The ICS template is a landscape "2-up" form: two identical copies side by
    // side on one sheet, meant to be cut down the centre gutter. Landscape is the
    // default so the two-copy layout fits; portrait would clip the right copy.
    private static async Task<IResult> PrintFast(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null)
    {
        var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait"
            ? "portrait"
            : "landscape";
        var rows = Math.Clamp(minRows ?? 15, 1, 40);

        var dto = await mediator.Send(
            new PrintICSFastQuery(id, paperSize, orient, rows), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
