using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintParStickers;

internal static class PrintParStickersEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/par/{accountabilityId:guid}/stickers/pdf",
            async (Guid accountabilityId, IMediator mediator, CancellationToken ct, string? pageWidth) =>
            {
                var paperSize = string.IsNullOrWhiteSpace(pageWidth) ? "longbond" : pageWidth.ToLowerInvariant();

                var bytes = await mediator.Send(new PrintParStickersQuery(accountabilityId, paperSize), ct);
                return Results.File(bytes, "application/pdf", "PAR-Stickers.pdf");
            })
            .WithName("QuestPdfReporting_PrintParStickers")
            .WithSummary("Generate property stickers (one per line, each with a Property-No QR code) for a PAR")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Accountability.View);
}
