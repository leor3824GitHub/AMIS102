using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.GetProduct;

public static class GetProductEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", GetProduct)
            .WithName(nameof(GetProductQuery))
            .WithSummary("Get product by ID")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.View);

    private static async Task<IResult> GetProduct(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetProductQuery(id);
        var result = await mediator.Send(query, cancellationToken);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }
}


