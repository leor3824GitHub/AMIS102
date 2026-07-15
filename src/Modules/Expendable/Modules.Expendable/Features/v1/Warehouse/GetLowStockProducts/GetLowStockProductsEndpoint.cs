using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Expendable.Contracts.Permissions;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetLowStockProducts;

public static class GetLowStockProductsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/low-stock", GetLowStock)
            .WithName("Expendable_GetLowStockProducts")
            .WithSummary("Products at or below their minimum stock level (reorder worklist)")
            .Produces<IReadOnlyList<LowStockProductDto>>(StatusCodes.Status200OK)
            .RequirePermission(ExpendablePermissions.Inventory.View);

    private static async Task<IResult> GetLowStock(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetLowStockProductsQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
