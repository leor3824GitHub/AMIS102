using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetWarehouseStockLevels;

public static class GetWarehouseStockLevelsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/warehouse/{warehouseLocationId:guid}/stock-levels", GetStockLevels)
            .WithName(nameof(GetWarehouseStockLevelsQuery))
            .WithSummary("Get warehouse stock levels")
            .Produces<PagedResponse<ProductInventoryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.ViewReports);

    private static async Task<IResult> GetStockLevels(
        Guid warehouseLocationId,
        [AsParameters] GetWarehouseStockLevelsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        query.WarehouseLocationId = warehouseLocationId;
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}


