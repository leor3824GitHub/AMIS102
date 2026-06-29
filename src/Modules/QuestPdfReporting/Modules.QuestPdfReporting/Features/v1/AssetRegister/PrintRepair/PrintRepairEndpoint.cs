using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRepair;

internal static class PrintRepairEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/repairs/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 14d;
                var bytes = await mediator.Send(new PrintRepairQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "RPRI.pdf");
            })
            .WithName("QuestPdfReporting_PrintRepair")
            .WithSummary("Generate the Request for Pre/Post Repair Inspection (RPRI, Exhibit 6) PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(AssetRegisterPermissions.Repair.View);
}
