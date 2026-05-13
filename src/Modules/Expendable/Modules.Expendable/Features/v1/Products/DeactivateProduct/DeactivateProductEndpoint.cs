using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Products.DeactivateProduct;

public static class DeactivateProductEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/deactivate", DeactivateProduct)
            .WithName(nameof(DeactivateProductCommand))
            .WithSummary("Deactivate a product")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Products.Deactivate);

    private static async Task<IResult> DeactivateProduct(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateProductCommand(id);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


