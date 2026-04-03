using FSH.Modules.Expendable.Contracts.v1.Warehouse;
using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Reports.GetStockCard;

public static class GetStockCardEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/stock-card/{productId:guid}", GetStockCard)
            .WithName(nameof(GetStockCardQuery))
            .WithSummary("Complete stock card ledger for a product — all receipts and issuances with running balance")
            .Produces<StockCardDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> GetStockCard(
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStockCardQuery(productId), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
