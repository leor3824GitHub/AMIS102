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
    // ?marginMm=<page margin in millimetres>  (default 15)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        Guid? warehouseLocationId = null,
        DateTime? asOfDate = null,
        DateTime? assumedAccountabilityDate = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 15d;

        var bytes = await mediator.Send(
            new PrintPhysicalCountQuery(warehouseLocationId, asOfDate, assumedAccountabilityDate, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "PhysicalCount.pdf");
    }
}
