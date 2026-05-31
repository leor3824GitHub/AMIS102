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

    private static async Task<IResult> Print(
        Guid productId,
        IMediator mediator,
        CancellationToken ct)
    {
        var bytes = await mediator.Send(new PrintStockCardQuery(productId), ct);
        return TypedResults.File(bytes, "application/pdf", "StockCard.pdf");
    }
}
