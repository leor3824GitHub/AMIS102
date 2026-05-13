using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Cart.GetCart;

public static class GetCartEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{cartId:guid}", GetCart)
            .WithName(nameof(GetCartQuery))
            .WithSummary("Get shopping cart by ID")
            .Produces<EmployeeShoppingCartDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.ShoppingCarts.View);

    private static async Task<IResult> GetCart(
        Guid cartId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetCartQuery(cartId);
        var result = await mediator.Send(query, cancellationToken);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }
}


