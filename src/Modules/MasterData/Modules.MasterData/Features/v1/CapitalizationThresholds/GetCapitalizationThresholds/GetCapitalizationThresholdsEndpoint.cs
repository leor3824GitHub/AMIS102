using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.GetCapitalizationThresholds;

public static class GetCapitalizationThresholdsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetCapitalizationThresholdsQuery(), ct)))
        .WithName(nameof(GetCapitalizationThresholdsQuery))
        .WithSummary("List all capitalization threshold records (COA circular history)")
        .RequirePermission(MasterDataModuleConstants.Permissions.CapitalizationThresholds.View);
}
