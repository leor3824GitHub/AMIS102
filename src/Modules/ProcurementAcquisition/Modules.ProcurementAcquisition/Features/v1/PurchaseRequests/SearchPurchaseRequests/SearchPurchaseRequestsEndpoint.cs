using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SearchPurchaseRequests;

public static class SearchPurchaseRequestsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", SearchPurchaseRequests)
            .WithName(nameof(SearchPurchaseRequestsQuery))
            .WithSummary("Search purchase requests with pagination")
            .Produces<PagedResponse<PurchaseRequestSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.PurchaseRequests.View);

    private static async Task<IResult> SearchPurchaseRequests(
        [AsParameters] SearchPurchaseRequestsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
