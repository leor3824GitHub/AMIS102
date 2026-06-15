using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertyCard;

internal static class PrintPropertyCardEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/property-card/pdf",
            async (IMediator mediator, CancellationToken ct, string propertyNo,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                if (string.IsNullOrWhiteSpace(propertyNo))
                    return Results.BadRequest("propertyNo is required.");
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
                var margin = marginMm is > 0 ? marginMm.Value : 12d;
                var bytes = await mediator.Send(new PrintPropertyCardQuery(propertyNo, paperSize, orient, margin), ct);
                return Results.File(bytes, "application/pdf", "PropertyCard.pdf");
            })
            .WithName("QuestPdfReporting_PrintPropertyCard")
            .WithSummary("Generate the Property Card (movement history) PDF for an asset")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Assets.View);
}
