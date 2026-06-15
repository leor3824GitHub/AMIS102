using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintUnserviceable;

internal static class PrintUnserviceableEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/unserviceable/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
                var margin = marginMm is > 0 ? marginMm.Value : 12d;
                var bytes = await mediator.Send(new PrintUnserviceableQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "IIRUP.pdf");
            })
            .WithName("QuestPdfReporting_PrintUnserviceable")
            .WithSummary("Generate the IIRUP/IIRUSP (unserviceable property) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Unserviceable.View);
}
