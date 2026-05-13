using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportList;

public static class GetPropertyIncidentReportListEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetList)
            .WithName(nameof(GetPropertyIncidentReportListQuery))
            .WithSummary("Get a paginated list of Property Incident Reports (RLSDDSP)")
            .Produces<PagedPropertyIncidentReportListResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.PropertyIncidentReports.View);

    private static async Task<IResult> GetList(
        [AsParameters] GetPropertyIncidentReportListQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

