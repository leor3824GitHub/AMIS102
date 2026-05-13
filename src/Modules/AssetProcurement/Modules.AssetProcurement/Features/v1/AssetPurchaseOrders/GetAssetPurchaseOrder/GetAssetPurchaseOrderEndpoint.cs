using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.GetAssetPurchaseOrder;

public static class GetAssetPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", Handle)
            .WithName(nameof(GetAssetPurchaseOrderQuery))
            .WithSummary("Get asset purchase order by ID")
            .Produces<AssetPurchaseOrderDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseOrders.View);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssetPurchaseOrderQuery(id), cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}

