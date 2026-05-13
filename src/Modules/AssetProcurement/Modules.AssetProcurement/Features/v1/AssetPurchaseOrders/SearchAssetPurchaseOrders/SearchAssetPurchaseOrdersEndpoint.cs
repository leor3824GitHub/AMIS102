using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.SearchAssetPurchaseOrders;

public static class SearchAssetPurchaseOrdersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchAssetPurchaseOrdersQuery))
            .WithSummary("Search asset purchase orders")
            .Produces<PagedResponse<AssetPurchaseOrderSummaryDto>>()
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseOrders.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAssetPurchaseOrdersQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

