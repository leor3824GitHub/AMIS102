using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

internal static class PrintPhysicalCountEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/physical-count/pdf", Print)
            .WithName("QuestPdfReporting_PrintPhysicalCount")
            .WithSummary("Generate a QuestPDF for the Physical Count report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewExpenditureReports);

    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        Guid? warehouseLocationId = null,
        DateTime? asOfDate = null)
    {
        var bytes = await mediator.Send(new PrintPhysicalCountQuery(warehouseLocationId, asOfDate), ct);
        return TypedResults.File(bytes, "application/pdf", "PhysicalCount.pdf");
    }
}
