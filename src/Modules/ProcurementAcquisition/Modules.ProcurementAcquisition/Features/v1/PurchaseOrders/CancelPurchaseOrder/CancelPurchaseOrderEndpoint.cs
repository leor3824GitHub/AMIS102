using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CancelPurchaseOrder;

public static class CancelPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatch("/{id:guid}/cancel", CancelPurchaseOrder)
            .WithName(nameof(CancelPurchaseOrderCommand))
            .WithSummary("Cancel a purchase order")
            .Produces<PurchaseOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseOrders.Cancel);

    private static async Task<IResult> CancelPurchaseOrder(
        Guid id,
        CancelPurchaseOrderRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelPurchaseOrderCommand(id, request.Reason), cancellationToken);
        return TypedResults.Ok(result);
    }
}

public sealed record CancelPurchaseOrderRequest(string? Reason = null);
