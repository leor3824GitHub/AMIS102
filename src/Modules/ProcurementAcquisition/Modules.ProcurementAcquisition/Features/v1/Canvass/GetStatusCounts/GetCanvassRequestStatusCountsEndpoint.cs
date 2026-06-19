using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetStatusCounts;

public static class GetCanvassRequestStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", GetStatusCounts)
            .WithName($"Procurement.{nameof(GetCanvassRequestStatusCountsQuery)}")
            .WithSummary("Get canvass request counts grouped by status")
            .Produces<IReadOnlyList<CanvassRequestStatusCountDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.CanvassRequests.View);

    private static async Task<IResult> GetStatusCounts(
        [AsParameters] GetCanvassRequestStatusCountsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
