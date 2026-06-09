using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetMyProductRating;

public static class GetMyProductRatingEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{productId:guid}/ratings/mine", GetMine)
            .WithName("Expendable_GetMyProductRating")
            .WithSummary("Get the current user's rating for a product (null if not yet rated).")
            .Produces<MyProductRatingDto>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetMine(
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyProductRatingQuery(productId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
