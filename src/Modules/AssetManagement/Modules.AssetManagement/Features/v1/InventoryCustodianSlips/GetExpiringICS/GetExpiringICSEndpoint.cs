using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetExpiringICS;

public static class GetExpiringICSEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/expiring", GetExpiringICS)
            .WithName(nameof(GetExpiringICSQuery))
            .WithSummary("Get Active ICS records expiring within the specified number of days")
            .Produces<PagedExpiringICSResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.InventoryCustodianSlips.View);

    private static async Task<IResult> GetExpiringICS(
        [AsParameters] GetExpiringICSQuery query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    }
}
