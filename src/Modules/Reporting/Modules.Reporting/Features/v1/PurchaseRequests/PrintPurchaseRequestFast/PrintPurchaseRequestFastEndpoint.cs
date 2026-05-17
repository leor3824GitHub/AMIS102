using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Reporting.Features.v1.PurchaseRequests.PrintPurchaseRequestFast;

public static class PrintPurchaseRequestFastEndpoint
{
    private const string PurchaseRequestsView = "Permissions.Procurement.PurchaseRequests.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print/fast", PrintFast)
            .WithName("Reporting_PrintPurchaseRequestFast")
            .WithSummary("Generate a landscape FastReport PDF for a purchase request (1 or 2 copies per page)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PurchaseRequestsView);

    // Query: ?pageWidth=a4|legal|longbond|letter   (default a4)
    //        ?copies=1|2                           (default 2)
    //        ?orientation=landscape|portrait       (default landscape)
    //        ?minRows=1..20                        (default 10) — pads data band with empty rows
    private static async Task<IResult> PrintFast(
        Guid id,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth = null,
        int? copies = null,
        string? orientation = null,
        int? minRows = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var copyCount = copies is 1 ? 1 : 2;
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait"
            ? "portrait"
            : "landscape";
        var rows = Math.Clamp(minRows ?? 10, 1, 20);

        var bytes = await mediator.Send(
            new PrintPurchaseRequestFastQuery(id, paperSize, copyCount, orient, rows), ct);

        return Results.File(bytes, "application/pdf", $"PR-{id}-fast.pdf");
    }
}
