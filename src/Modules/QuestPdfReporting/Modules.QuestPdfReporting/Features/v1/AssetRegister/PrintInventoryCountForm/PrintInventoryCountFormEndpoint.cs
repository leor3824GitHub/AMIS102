using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintInventoryCountForm;

internal static class PrintInventoryCountFormEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints)
    {
        // ?pageWidth=a4|legal|longbond|letter (default a4)  ?orientation=portrait|landscape (default landscape)
        // ?marginMm=<page margin in mm> (default 12)
        endpoints.MapGet("/physical-count/{sessionId:guid}/icf/pdf",
            async (Guid sessionId, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
                var margin = marginMm is > 0 ? marginMm.Value : 12d;

                var bytes = await mediator.Send(new PrintInventoryCountFormQuery(sessionId, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "InventoryCountForm-AnnexA.pdf");
            })
            .WithName("QuestPdfReporting_PrintInventoryCountForm")
            .WithSummary("Generate the Inventory Count Form (Annex A) PDF for a physical count session")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Count.View);
    }
}
