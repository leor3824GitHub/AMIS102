using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementPlanning.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.ProcurementPlanning.PrintApp;

internal static class PrintAppEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/apps/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "longbond").ToLowerInvariant();
                var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
                var margin = marginMm is > 0 ? marginMm.Value : 8d;
                var bytes = await mediator.Send(new PrintAppQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "AnnualProcurementPlan.pdf");
            })
            .WithName("QuestPdfReporting_PrintAnnualProcurementPlan")
            .WithSummary("Generate the Annual Procurement Plan (APP) PDF in the GPPB form layout")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(ProcurementPlanningPermissions.AnnualProcurementPlans.View);
}
