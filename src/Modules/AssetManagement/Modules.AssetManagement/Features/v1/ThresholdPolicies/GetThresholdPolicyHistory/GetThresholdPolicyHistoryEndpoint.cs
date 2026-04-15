using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.ThresholdPolicies.GetThresholdPolicyHistory;

public static class GetThresholdPolicyHistoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", GetThresholdPolicyHistory)
            .WithName(nameof(GetThresholdPolicyHistoryQuery))
            .WithSummary("Get paginated history of all capitalization threshold policies")
            .Produces<PagedThresholdPolicyHistoryResponse>(StatusCodes.Status200OK)
            .RequirePermission(AssetManagementModuleConstants.Permissions.ThresholdPolicies.View);

    private static async Task<IResult> GetThresholdPolicyHistory(
        int pageNumber = 1,
        int pageSize = 20,
        IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetThresholdPolicyHistoryQuery(pageNumber, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }
}
