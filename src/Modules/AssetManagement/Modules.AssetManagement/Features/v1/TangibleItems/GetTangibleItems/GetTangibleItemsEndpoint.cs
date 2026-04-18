using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItems;

public static class GetTangibleItemsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetTangibleItems)
            .WithName(nameof(GetTangibleItemsQuery))
            .WithSummary("Get paginated list of tangible items")
            .Produces<PagedTangibleItemsResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.TangibleItems.View);

    private static async Task<IResult> GetTangibleItems(
        string? keyword = null,
        bool? excludeLinked = null,
        int pageNumber = 1,
        int pageSize = 20,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTangibleItemsQuery(keyword, excludeLinked, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
