using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.RemovePurchaseLineItem;

public static class RemovePurchaseLineItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{purchaseId:guid}/items/{productId:guid}", RemoveLineItem)
            .WithName(nameof(RemovePurchaseLineItemCommand))
            .WithSummary("Remove a line item from a purchase order")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.Update);

    private static async Task<IResult> RemoveLineItem(
        Guid purchaseId,
        Guid productId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new RemovePurchaseLineItemCommand(purchaseId, productId);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


