using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.SetThresholdPolicy;

public static class SetThresholdPolicyEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", SetThresholdPolicy)
            .WithName(nameof(SetThresholdPolicyCommand))
            .WithSummary("Set a new capitalization threshold policy, superseding the current one")
            .Produces<SetThresholdPolicyResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ThresholdPolicies.Create);

    private static async Task<IResult> SetThresholdPolicy(
        SetThresholdPolicyCommand command,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return TypedResults.Created($"/api/v1/asset-management/threshold-policies/{result.PolicyId}", result);
    }
}
