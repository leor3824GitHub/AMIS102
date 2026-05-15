using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.IssuePurchaseOrder;

public static class IssuePurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatch("/{id:guid}/issue", IssuePurchaseOrder)
            .WithName($"Procurement.{nameof(IssuePurchaseOrderCommand)}")
            .WithSummary("Issue a draft purchase order")
            .Produces<PurchaseOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseOrders.Issue);

    private static async Task<IResult> IssuePurchaseOrder(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new IssuePurchaseOrderCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

