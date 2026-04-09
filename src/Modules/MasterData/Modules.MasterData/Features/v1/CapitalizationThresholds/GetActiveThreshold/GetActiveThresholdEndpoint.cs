using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.GetActiveThreshold;

public static class GetActiveThresholdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/active", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetActiveCapitalizationThresholdQuery(), ct);
            return result is null ? Results.NotFound() : TypedResults.Ok(result);
        })
        .WithName(nameof(GetActiveCapitalizationThresholdQuery))
        .WithSummary("Get the currently active capitalization threshold (COA circular)")
        .RequirePermission(MasterDataModuleConstants.Permissions.CapitalizationThresholds.View);
}
