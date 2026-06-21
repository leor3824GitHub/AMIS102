using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

internal static class PrintPropertyStickerEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/property-sticker/pdf",
            async (IMediator mediator, CancellationToken ct, string propertyNo, string? pageWidth) =>
            {
                if (string.IsNullOrWhiteSpace(propertyNo))
                    return Results.BadRequest("propertyNo is required.");

                var paperSize = string.IsNullOrWhiteSpace(pageWidth) ? "longbond" : pageWidth.ToLowerInvariant();

                var bytes = await mediator.Send(new PrintPropertyStickerQuery(propertyNo, paperSize), ct);
                return Results.File(bytes, "application/pdf", "PropertySticker.pdf");
            })
            .WithName("QuestPdfReporting_PrintPropertySticker")
            .WithSummary("Generate the printable property sticker (with Property-No QR code) PDF for an asset")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Assets.View);
}
