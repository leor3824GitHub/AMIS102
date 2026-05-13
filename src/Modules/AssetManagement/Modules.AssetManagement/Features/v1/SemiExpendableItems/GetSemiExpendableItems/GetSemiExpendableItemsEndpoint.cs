using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;

public static class GetSemiExpendableItemsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetItemCatalog)
            .WithName("AssetManagement_GetSemiExpendableItems")
            .WithSummary("Get paginated list of item catalog entries")
            .Produces<PagedPropertyItemCatalogResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.SemiExpendableItems.View);

    private static async Task<IResult> GetItemCatalog(
        string? keyword = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPropertyItemCatalogQuery(keyword, isActive, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

