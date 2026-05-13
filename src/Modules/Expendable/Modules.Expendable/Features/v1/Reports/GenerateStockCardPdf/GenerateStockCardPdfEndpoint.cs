using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.v1.Reports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Reports.GenerateStockCardPdf;

public static class GenerateStockCardPdfEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/stock-card/pdf", Generate)
            .WithName(nameof(GenerateStockCardPdfCommand))
            .WithSummary("Generate a PDF for the Stock Card report")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> Generate(
        GenerateStockCardPdfCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var bytes = await mediator.Send(command, cancellationToken);
        return TypedResults.File(bytes, "application/pdf", "StockCard.pdf");
    }
}

