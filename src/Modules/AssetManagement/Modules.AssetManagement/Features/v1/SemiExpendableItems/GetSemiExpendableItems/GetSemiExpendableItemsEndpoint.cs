using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;

public static class GetSemiExpendableItemsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetSemiExpendableItems)
            .WithName(nameof(GetSemiExpendableItemsQuery))
            .WithSummary("Get paginated list of semi-expendable item catalog entries")
            .Produces<PagedSemiExpendableItemsResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.View);

    private static async Task<IResult> GetSemiExpendableItems(
        string? keyword = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSemiExpendableItemsQuery(keyword, isActive, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
