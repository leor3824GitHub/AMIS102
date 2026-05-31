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

    // ?pageWidth=a4|legal|longbond|letter   (default a4)
    // ?orientation=landscape|portrait        (default landscape)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        Guid? warehouseLocationId = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        string? orientation = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";

        var bytes = await mediator.Send(
            new PrintPhysicalCountQuery(warehouseLocationId, asOfDate, paperSize, orient), ct);
        return TypedResults.File(bytes, "application/pdf", "PhysicalCount.pdf");
    }
}
