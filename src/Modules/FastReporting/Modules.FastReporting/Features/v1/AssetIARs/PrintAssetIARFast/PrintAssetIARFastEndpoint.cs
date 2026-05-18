using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.AssetIARs.PrintAssetIARFast;

public static class PrintAssetIARFastEndpoint
{
    private const string AssetIARsView = "Permissions.Procurement.AssetIARs.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintAssetIAR")
            .WithSummary("Generate a FastReport PDF for an Inspection and Acceptance Report (IAR)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetIARsView);

    // Query: ?pageWidth=a4|legal|longbond|letter   (default a4)
    //        ?orientation=portrait|landscape       (default portrait)
    //        ?minRows=1..30                        (default 12) — pads data band with empty rows
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
        var rows = Math.Clamp(minRows ?? 12, 1, 30);

        var dto = await mediator.Send(
            new PrintAssetIARFastQuery(id, paperSize, orient, rows), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
