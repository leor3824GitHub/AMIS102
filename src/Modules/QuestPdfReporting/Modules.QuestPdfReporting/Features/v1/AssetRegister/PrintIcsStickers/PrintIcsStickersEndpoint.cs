using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIcsStickers;

internal static class PrintIcsStickersEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/ics/{accountabilityId:guid}/stickers/pdf",
            async (Guid accountabilityId, IMediator mediator, CancellationToken ct, string? pageWidth) =>
            {
                var paperSize = string.IsNullOrWhiteSpace(pageWidth) ? "longbond" : pageWidth.ToLowerInvariant();

                var bytes = await mediator.Send(new PrintIcsStickersQuery(accountabilityId, paperSize), ct);
                return Results.File(bytes, "application/pdf", "ICS-Stickers.pdf");
            })
            .WithName("QuestPdfReporting_PrintIcsStickers")
            .WithSummary("Generate property stickers (one per line, each with a Property-No QR code) for an ICS")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Accountability.View);
}
