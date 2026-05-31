using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Contracts.v1.Reports;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRSPI;

internal static class PrintRSPIEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/rspi/pdf", Print)
            .WithName("QuestPdfReporting_PrintRSPI")
            .WithSummary("Generate a QuestPDF for the Report of Semi-Expendable Property Issued (RSPI)")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(QuestPdfReportingPermissions.ViewAssetReports);

    // ?pageWidth=a4|legal|longbond|letter   (default a4)
    // ?orientation=landscape|portrait        (default landscape)
    private static async Task<IResult> Print(
        IMediator mediator,
        CancellationToken ct,
        DateOnly?  dateFrom    = null,
        DateOnly?  dateTo      = null,
        AssetType? assetType   = null,
        bool       activeOnly  = false,
        int        pageNumber  = 1,
        int        pageSize    = 10000,
        string?    pageWidth   = null,
        string?    orientation = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";

        var bytes = await mediator.Send(
            new PrintRSPIQuery(dateFrom, dateTo, assetType, activeOnly, pageNumber, pageSize, paperSize, orient), ct);
        return TypedResults.File(bytes, "application/pdf", "RSPIReport.pdf");
    }
}
