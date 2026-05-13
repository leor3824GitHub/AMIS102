using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SearchPurchaseOrders;

public static class SearchPurchaseOrdersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", SearchPurchaseOrders)
            .WithName(nameof(SearchPurchaseOrdersQuery))
            .WithSummary("Search purchase orders with pagination")
            .Produces<PagedResponse<PurchaseOrderSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseOrders.View);

    private static async Task<IResult> SearchPurchaseOrders(
        [AsParameters] SearchPurchaseOrdersQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

