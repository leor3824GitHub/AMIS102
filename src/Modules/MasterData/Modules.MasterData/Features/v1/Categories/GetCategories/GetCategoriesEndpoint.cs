using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.Categories.GetCategories;

public static class GetCategoriesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetCategories)
            .WithName(nameof(GetCategoriesQuery))
            .WithSummary("Get paginated list of categories")
            .Produces<PagedResponseOfCategoryDto>(StatusCodes.Status200OK)
            .RequirePermission(MasterDataModuleConstants.Permissions.Categories.View);

    private static async Task<IResult> GetCategories(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 10,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCategoriesQuery(keyword, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

