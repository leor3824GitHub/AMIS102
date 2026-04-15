using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;

public static class GetRSPIEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/rspi", GetRSPI)
            .WithName(nameof(GetRSPIQuery))
            .WithSummary("Report of Semi-Expendable Property Issued (RSPI) — paginated list of issued properties for a period")
            .Produces<PagedRSPIResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.Reports.View);

    private static async Task<IResult> GetRSPI(
        [AsParameters] GetRSPIQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
