using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SearchJobOrders;

public static class SearchJobOrdersEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", SearchJobOrders)
            .WithName($"Procurement.{nameof(SearchJobOrdersQuery)}")
            .WithSummary("Search job orders with pagination")
            .Produces<PagedResponse<JobOrderSummaryDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.JobOrders.View);

    private static async Task<IResult> SearchJobOrders(
        [AsParameters] SearchJobOrdersQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
