using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.UpdatePurchaseOrder;

public static class UpdatePurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", UpdatePurchaseOrder)
            .WithName($"Procurement.{nameof(UpdatePurchaseOrderCommand)}")
            .WithSummary("Update a draft purchase order")
            .Produces<PurchaseOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseOrders.Update);

    private static async Task<IResult> UpdatePurchaseOrder(
        Guid id,
        UpdatePurchaseOrderCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}

