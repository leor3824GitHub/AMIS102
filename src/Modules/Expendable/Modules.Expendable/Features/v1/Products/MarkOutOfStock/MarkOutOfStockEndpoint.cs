using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Products.MarkOutOfStock;

public static class MarkOutOfStockEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/out-of-stock", MarkOutOfStock)
            .WithName(nameof(MarkOutOfStockCommand))
            .WithSummary("Mark a product as out of stock")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.MarkOutOfStock);

    private static async Task<IResult> MarkOutOfStock(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkOutOfStockCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
