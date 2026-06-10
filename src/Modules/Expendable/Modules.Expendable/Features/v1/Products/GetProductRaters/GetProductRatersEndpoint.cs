using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRaters;

public static class GetProductRatersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{productId:guid}/ratings/raters", GetRaters)
            .WithName("Expendable_GetProductRaters")
            .WithSummary("Get the individual raters (name + value) for a single product.")
            .Produces<List<ProductRaterDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetRaters(
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductRatersQuery(productId), cancellationToken);
        return TypedResults.Ok(result);
    }
}