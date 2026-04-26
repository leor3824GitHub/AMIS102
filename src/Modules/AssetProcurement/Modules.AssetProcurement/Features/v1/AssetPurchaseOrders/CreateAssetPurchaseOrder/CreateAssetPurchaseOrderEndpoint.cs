using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;

public static class CreateAssetPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", Handle)
            .WithName(nameof(CreateAssetPurchaseOrderCommand))
            .WithSummary("Create a new asset purchase order")
            .Produces<AssetPurchaseOrderDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseOrders.Create);

    private static async Task<IResult> Handle(
        CreateAssetPurchaseOrderCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-procurement/purchase-orders/{result.Id}", result);
    }
}
