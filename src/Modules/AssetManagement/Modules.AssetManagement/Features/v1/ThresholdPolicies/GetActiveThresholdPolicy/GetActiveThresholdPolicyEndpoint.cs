using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetActiveThresholdPolicy;

public static class GetActiveThresholdPolicyEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/active", GetActiveThresholdPolicy)
            .WithName(nameof(GetActiveThresholdPolicyQuery))
            .WithSummary("Get the currently active capitalization threshold policy")
            .Produces<ThresholdPolicyDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ThresholdPolicies.View);

    private static async Task<IResult> GetActiveThresholdPolicy(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetActiveThresholdPolicyQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }
}
