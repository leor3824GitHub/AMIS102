using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.MasterData.Contracts.Permissions;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.GetCapitalizationThresholds;

public static class GetCapitalizationThresholdsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetCapitalizationThresholdsQuery(), ct)))
        .WithName(nameof(GetCapitalizationThresholdsQuery))
        .WithSummary("List all capitalization threshold records (COA circular history)")
        .RequirePermission(MasterDataPermissions.CapitalizationThresholds.View);
}

