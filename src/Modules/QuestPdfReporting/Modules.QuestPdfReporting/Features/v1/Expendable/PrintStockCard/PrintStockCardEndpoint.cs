using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.QuestPdfReporting.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintStockCard;

internal static class PrintStockCardEndpoint
{
    internal static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/stock-card/{productId:guid}/pdf", Print)
            .WithName("QuestPdfReporting_PrintStockCard")
            .WithSummary("Generate a QuestPDF for the Stock Card report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(QuestPdfReportingPermissions.ViewExpenditureReports);

    // ?pageWidth=a4|legal|longbond|letter   (default a4)
    // ?orientation=landscape|portrait        (default landscape)
    // ?marginMm=<page margin in millimetres>  (default 15)
    private static async Task<IResult> Print(
        Guid productId,
        IMediator mediator,
        CancellationToken ct,
        string? pageWidth   = null,
        string? orientation = null,
        double? marginMm    = null)
    {
        var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
        var orient = (orientation ?? "landscape").ToLowerInvariant() == "portrait" ? "portrait" : "landscape";
        var margin = marginMm is > 0 ? marginMm.Value : 15d;

        var bytes = await mediator.Send(new PrintStockCardQuery(productId, paperSize, orient, margin), ct);
        return TypedResults.File(bytes, "application/pdf", "StockCard.pdf");
    }
}
