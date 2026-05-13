using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Cart.GetOrCreateCart;

public static class GetOrCreateCartEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/employee/{employeeId}/cart", GetOrCreate)
            .WithName(nameof(GetOrCreateCartCommand))
            .WithSummary("Get or create employee shopping cart")
            .Produces<EmployeeShoppingCartDto>(StatusCodes.Status200OK)
            .Produces<EmployeeShoppingCartDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.ShoppingCarts.View);

    private static async Task<IResult> GetOrCreate(
        string employeeId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new GetOrCreateCartCommand(employeeId);
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Ok(result);
    }
}


