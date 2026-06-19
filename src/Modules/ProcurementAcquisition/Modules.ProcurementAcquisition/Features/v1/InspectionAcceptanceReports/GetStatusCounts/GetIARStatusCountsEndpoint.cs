using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetStatusCounts;

public static class GetIARStatusCountsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/status-counts", GetStatusCounts)
            .WithName($"Procurement.{nameof(GetIARStatusCountsQuery)}")
            .WithSummary("Get inspection & acceptance report counts grouped by status")
            .Produces<IReadOnlyList<IARStatusCountDto>>(StatusCodes.Status200OK)
            .RequirePermission(ProcurementPermissions.InspectionAcceptanceReports.View);

    private static async Task<IResult> GetStatusCounts(
        [AsParameters] GetIARStatusCountsQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
