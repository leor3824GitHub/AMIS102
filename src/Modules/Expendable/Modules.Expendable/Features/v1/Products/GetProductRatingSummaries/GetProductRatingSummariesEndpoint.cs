using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductRatingSummaries;

public static class GetProductRatingSummariesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/ratings/summaries", GetSummaries)
            .WithName("Expendable_GetProductRatingSummaries")
            .WithSummary("Get rating summaries (average + count) for all products in the tenant.")
            .Produces<List<ProductRatingSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetSummaries(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductRatingSummariesQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
