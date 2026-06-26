using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Procurement.PrintJobOrder;

internal static class PrintJobOrderEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/job-orders/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 10d;
                var bytes = await mediator.Send(new PrintJobOrderQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "JobOrder.pdf");
            })
            .WithName("QuestPdfReporting_PrintJobOrder")
            .WithSummary("Generate the Job Order (JO) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(ProcurementPermissions.JobOrders.View);
}
