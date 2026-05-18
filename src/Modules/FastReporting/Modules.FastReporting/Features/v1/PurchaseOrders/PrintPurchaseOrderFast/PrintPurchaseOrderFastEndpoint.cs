using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.FastReporting.Features.v1.PurchaseOrders.PrintPurchaseOrderFast;

public static class PrintPurchaseOrderFastEndpoint
{
    private const string PurchaseOrdersView = "Permissions.Procurement.PurchaseOrders.View";

    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}/print", PrintFast)
            .WithName("FastReporting_PrintPurchaseOrder")
            .WithSummary("Generate a FastReport PDF for a purchase order (1 PO per page)")
            .Produces(StatusCodes.Status200OK, null, "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PurchaseOrdersView);

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
            new PrintPurchaseOrderFastQuery(id, paperSize, orient, rows), ct);

        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }
}
