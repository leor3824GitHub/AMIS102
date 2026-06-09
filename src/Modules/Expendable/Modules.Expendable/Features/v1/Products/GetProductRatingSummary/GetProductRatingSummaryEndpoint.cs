using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRatingSummary;

public static class GetProductRatingSummaryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{productId:guid}/ratings", GetSummary)
            .WithName("Expendable_GetProductRatingSummary")
            .WithSummary("Get the rating summary (average + count) for a single product.")
            .Produces<ProductRatingSummaryDto>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetSummary(
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductRatingSummaryQuery(productId), cancellationToken);
        return TypedResults.Ok(result);
    }
}
