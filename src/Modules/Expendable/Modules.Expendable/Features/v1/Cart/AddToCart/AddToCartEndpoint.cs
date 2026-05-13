using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Cart.AddToCart;

public static class AddToCartEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{cartId:guid}/items", AddItem)
            .WithName(nameof(AddToCartCommand))
            .WithSummary("Add item to cart")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.ShoppingCarts.Edit);

    private static async Task<IResult> AddItem(
        Guid cartId,
        AddToCartCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { CartId = cartId };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


