using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.CancelProductInventoryReservation;

public static class CancelProductInventoryReservationEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/inventory/{inventoryId:guid}/cancel-reservation", CancelReservation)
            .WithName(nameof(CancelProductInventoryReservationCommand))
            .WithSummary("Cancel inventory reservation")
            .Produces<CancelProductInventoryReservationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Inventory.Receive);

    private static async Task<IResult> CancelReservation(
        Guid inventoryId,
        CancelProductInventoryReservationCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { ProductInventoryId = inventoryId };
        var result = await mediator.Send(cmd, cancellationToken);
        return TypedResults.Ok(result);
    }
}


