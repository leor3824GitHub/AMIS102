using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.IssueAssetPurchaseOrder;

public static class IssueAssetPurchaseOrderEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/issue", Handle)
            .WithName(nameof(IssueAssetPurchaseOrderCommand))
            .WithSummary("Issue an asset purchase order to the supplier")
            .Produces<AssetPurchaseOrderDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseOrders.Issue);

    private static async Task<IResult> Handle(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new IssueAssetPurchaseOrderCommand(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}

