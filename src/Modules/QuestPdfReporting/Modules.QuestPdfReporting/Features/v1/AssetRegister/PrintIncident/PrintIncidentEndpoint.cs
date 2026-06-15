using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintIncident;

internal static class PrintIncidentEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/incidents/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 14d;
                var bytes = await mediator.Send(new PrintIncidentQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "RLSDDSP.pdf");
            })
            .WithName("QuestPdfReporting_PrintIncident")
            .WithSummary("Generate the RLSDDSP (lost/stolen/damaged/destroyed property) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Incident.View);
}
