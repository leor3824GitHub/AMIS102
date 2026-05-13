using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.SearchCanvassRequests;

public static class SearchCanvassRequestsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", SearchCanvassRequests)
            .WithName(nameof(SearchCanvassRequestsQuery))
            .WithSummary("Search canvass requests with pagination")
            .Produces<PagedResponse<CanvassRequestSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementAcquisitionModuleConstants.Permissions.CanvassRequests.View);

    private static async Task<IResult> SearchCanvassRequests(
        [AsParameters] SearchCanvassRequestsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

