using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CancelAssetPurchaseOrder;

public static class CancelAssetPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/cancel", Handle)
            .WithName(nameof(CancelAssetPurchaseOrderCommand))
            .WithSummary("Cancel an asset purchase order")
            .Produces<AssetPurchaseOrderDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseOrders.Cancel);

    private static async Task<IResult> Handle(
        Guid id,
        CancelAssetPurchaseOrderCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command with { Id = id }, cancellationToken);
        return TypedResults.Ok(result);
    }
}
