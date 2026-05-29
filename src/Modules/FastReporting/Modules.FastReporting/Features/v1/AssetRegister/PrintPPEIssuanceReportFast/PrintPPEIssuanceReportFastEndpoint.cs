using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.AssetRegister.PrintPPEIssuanceReportFast;

public static class PrintPPEIssuanceReportFastEndpoint
{
    private const string IssuanceView = "Permissions.AssetRegister.Issuance.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintPPEIssuanceReport")
            .WithSummary("Generate a FastReport PDF for a PPE Issuance Report (PPEIR)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(IssuanceView);

    // Query: ?pageWidth=longbond|a4|legal|letter   (default longbond)
    //        ?orientation=portrait|landscape        (default portrait)
    //        ?minRows=1..30                         (default 12) — pads empty rows in the table
    //        ?dataOnly=true                         (default false) — overlay mode: print only
    //                                                 field values for pre-printed accountable forms
    //        ?offsetX, ?offsetY                     (mm, default 0) — calibration nudge for overlay
    private static async Task<IResult> PrintFast(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        bool? dataOnly = null,
        double? offsetX = null,
        double? offsetY = null)
    {
        var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape"
            ? "landscape"
            : "portrait";
        var rows = Math.Clamp(minRows ?? 12, 1, 30);
        var nudgeX = (float)Math.Clamp(offsetX ?? 0d, -30d, 30d);
        var nudgeY = (float)Math.Clamp(offsetY ?? 0d, -30d, 30d);

        var dto = await mediator.Send(
            new PrintPPEIssuanceReportFastQuery(
                id, paperSize, orient, rows, dataOnly ?? false, nudgeX, nudgeY), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
