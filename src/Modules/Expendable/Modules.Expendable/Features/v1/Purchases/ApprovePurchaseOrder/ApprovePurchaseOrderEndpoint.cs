using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Expendable.Features.v1.Purchases.ApprovePurchaseOrder;

public static class ApprovePurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/approve", ApprovePurchaseOrder)
            .WithName(nameof(ApprovePurchaseOrderCommand))
            .WithSummary("Approve a purchase order")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ExpendableModuleConstants.Permissions.Purchases.Approve);

    private static async Task<IResult> ApprovePurchaseOrder(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ApprovePurchaseOrderCommand(id);
        await mediator.Send(command, cancellationToken);
        return TypedResults.NoContent();
    }
}


