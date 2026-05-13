using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.SearchProducts;

public static class SearchProductsEndpoint
{
    public static void Map(this IEndpointRouteBuilder endpoints)
    {
        // Primary GET route—maps to /api/v1/expendable/products
        endpoints.MapGet("/", SearchFromQueryString)
            .WithName(nameof(SearchProductsQuery))
            .WithSummary("Search products")
            .Produces<PagedResponse<ProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.View);

        // Backward-compatible POST route for /search
        endpoints.MapPost("/search", SearchFromBody)
            .WithName($"{nameof(SearchProductsQuery)}Post")
            .WithSummary("Search products (legacy POST route)")
            .Produces<PagedResponse<ProductDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.View);

        // no-op
    }

    private static async Task<IResult> SearchFromQueryString(
        [AsParameters] SearchProductsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> SearchFromBody(
        SearchProductsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}


