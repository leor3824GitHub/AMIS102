using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.AddPurchaseLineItem;

public static class AddPurchaseLineItemEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{purchaseId:guid}/items", AddLineItem)
            .WithName(nameof(AddPurchaseLineItemCommand))
            .WithSummary("Add a line item to a purchase order")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.Update);

    private static async Task<IResult> AddLineItem(
        Guid purchaseId,
        AddPurchaseLineItemCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cmd = command with { PurchaseId = purchaseId };
        await mediator.Send(cmd, cancellationToken);
        return TypedResults.NoContent();
    }
}


