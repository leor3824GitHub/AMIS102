using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintReturnedProperty;

internal static class PrintReturnedPropertyEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/returned-property/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm, int? minRows) =>
            {
                var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 14d;
                var rows = Math.Clamp(minRows ?? 12, 1, 40);
                var bytes = await mediator.Send(new PrintReturnedPropertyQuery(id, paperSize, orient, margin, rows), ct);
                return TypedResults.File(bytes, "application/pdf", "ReturnedProperty.pdf");
            })
            .WithName("QuestPdfReporting_PrintReturnedProperty")
            .WithSummary("Generate the Receipt of Returned Property (RRP / RRSP) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.ReturnedProperty.View);
}
