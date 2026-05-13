using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.CancelPurchaseOrder;

public static class CancelPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/cancel", CancelPurchaseOrder)
            .WithName(nameof(CancelPurchaseOrderCommand))
            .WithSummary("Cancel a purchase order")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.Delete);

    private static async Task<IResult> CancelPurchaseOrder(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CancelPurchaseOrderCommand(id);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


