using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.BudgetDisbursement.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.BudgetDisbursement.PrintBudgetUtilizationRequest;

internal static class PrintBudgetUtilizationRequestEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/budget-utilization-requests/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 14d;
                var bytes = await mediator.Send(new PrintBudgetUtilizationRequestQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "BudgetUtilizationRequestAndStatus.pdf");
            })
            .WithName("QuestPdfReporting_PrintBudgetUtilizationRequest")
            .WithSummary("Generate the Budget Utilization Request and Status (BURS) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(BudgetDisbursementPermissions.BudgetUtilizationRequests.View);
}
