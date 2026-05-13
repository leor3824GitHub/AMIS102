using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Cart.ClearCart;

public static class ClearCartEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{cartId:guid}/clear", Clear)
            .WithName(nameof(ClearCartCommand))
            .WithSummary("Clear all items from cart")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.ShoppingCarts.Clear);

    private static async Task<IResult> Clear(
        Guid cartId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ClearCartCommand(cartId);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


