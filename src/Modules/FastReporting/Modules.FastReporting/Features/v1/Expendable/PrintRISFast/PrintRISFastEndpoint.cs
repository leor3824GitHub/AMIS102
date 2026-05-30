using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.Expendable.PrintRISFast;

public static class PrintRISFastEndpoint
{
    private const string SupplyRequestView = "Permissions.Expendable.SupplyRequests.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintRIS")
            .WithSummary("Generate a FastReport PDF for a Requisition and Issue Slip (RIS)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SupplyRequestView);

    // Query: ?pageWidth=longbond|a4|legal|letter   (default longbond)
    //        ?orientation=portrait|landscape         (default portrait)
    //        ?minRows=1..40                          (default 12) — pads empty rows in the table
    private static async Task<IResult> PrintFast(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null)
    {
        var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
        var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape"
            ? "landscape"
            : "portrait";
        var rows = Math.Clamp(minRows ?? 12, 1, 40);

        var dto = await mediator.Send(
            new PrintRISFastQuery(id, paperSize, orient, rows), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
