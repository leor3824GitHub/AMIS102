using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetProductInventory;

public static class GetProductInventoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/inventory/{productId:guid}/warehouse/{warehouseLocationId:guid}", GetInventory)
            .WithName(nameof(GetProductInventoryQuery))
            .WithSummary("Get product inventory by location")
            .Produces<ProductInventoryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.View);

    private static async Task<IResult> GetInventory(
        Guid productId,
        Guid warehouseLocationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetProductInventoryQuery(productId, warehouseLocationId);
        var result = await mediator.Send(query, cancellationToken);
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }
}


