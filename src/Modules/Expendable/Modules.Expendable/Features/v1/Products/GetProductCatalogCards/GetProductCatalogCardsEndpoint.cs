using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Products.GetProductCatalogCards;

public static class GetProductCatalogCardsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/cards", GetProductCatalogCards)
            .WithName(nameof(GetProductCatalogCardsQuery))
            .WithSummary("Get employee-facing product cards for catalog browsing")
            .Produces<PagedResponse<ProductCatalogCardDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.View);

    private static async Task<IResult> GetProductCatalogCards(
        [AsParameters] GetProductCatalogCardsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
