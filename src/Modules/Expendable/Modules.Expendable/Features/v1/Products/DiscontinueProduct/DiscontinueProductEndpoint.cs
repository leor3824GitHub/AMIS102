using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Expendable.Contracts.v1.Products;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Expendable.Features.v1.Products.DiscontinueProduct;

public static class DiscontinueProductEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/discontinue", DiscontinueProduct)
            .WithName(nameof(DiscontinueProductCommand))
            .WithSummary("Discontinue a product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.Discontinue);

    private static async Task<IResult> DiscontinueProduct(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DiscontinueProductCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
