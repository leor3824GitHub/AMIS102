using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.ReserveProductInventory;

public static class ReserveProductInventoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/inventory/{inventoryId:guid}/reserve", ReserveInventory)
            .WithName(nameof(ReserveProductInventoryCommand))
            .WithSummary("Reserve product inventory")
            .Produces<ReserveProductInventoryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Receive);

    private static async Task<IResult> ReserveInventory(
        Guid inventoryId,
        ReserveProductInventoryCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { ProductInventoryId = inventoryId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


