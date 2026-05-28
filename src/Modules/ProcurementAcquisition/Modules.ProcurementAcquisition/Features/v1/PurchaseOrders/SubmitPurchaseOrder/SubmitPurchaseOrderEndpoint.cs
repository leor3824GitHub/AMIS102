using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SubmitPurchaseOrder;

public static class SubmitPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/submit", SubmitPurchaseOrder)
            .WithName($"Procurement.{nameof(SubmitPurchaseOrderCommand)}")
            .WithSummary("Submit a draft purchase order for funds-available certification")
            .Produces<PurchaseOrderDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(ProcurementPermissions.PurchaseOrders.Submit);

    private static async Task<IResult> SubmitPurchaseOrder(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitPurchaseOrderCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
