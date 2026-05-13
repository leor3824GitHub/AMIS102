using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportList;

public static class GetUnserviceablePropertyReportListEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetList)
            .WithName(nameof(GetUnserviceablePropertyReportListQuery))
            .WithSummary("Get a paginated list of Inspection and Inventory Reports of Unserviceable Semi-Expendable Properties")
            .Produces<PagedUnserviceablePropertyReportListResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.UnserviceablePropertyReports.View);

    private static async Task<IResult> GetList(
        [AsParameters] GetUnserviceablePropertyReportListQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}

