using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Expendable.Contracts.Permissions;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProductArticles;

public static class GetProductArticlesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/articles", GetProductArticles)
            .WithName(nameof(GetProductArticlesQuery))
            .WithSummary("List distinct product articles for autocomplete")
            .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendablePermissions.Products.View);

    private static async Task<IResult> GetProductArticles(
        [AsParameters] GetProductArticlesQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
