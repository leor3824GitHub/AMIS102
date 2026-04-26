using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.SearchAssetPurchaseRequests;

public static class SearchAssetPurchaseRequestsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", Handle)
            .WithName(nameof(SearchAssetPurchaseRequestsQuery))
            .WithSummary("Search asset purchase requests")
            .Produces<PagedResponse<AssetPurchaseRequestSummaryDto>>()
            .RequirePermission(AssetProcurementModuleConstants.Permissions.AssetPurchaseRequests.View);

    private static async Task<IResult> Handle(
        [AsParameters] SearchAssetPurchaseRequestsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
