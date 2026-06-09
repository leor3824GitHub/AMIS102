using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.RateProduct;

public static class RateProductEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{productId:guid}/ratings", Rate)
            .WithName("Expendable_RateProduct")
            .WithSummary("Rate a product (1-5). Creates or updates the current user's rating.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendablePermissions.Products.Rate);

    private static async Task<IResult> Rate(
        Guid productId,
        RateProductCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { ProductId = productId };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}
