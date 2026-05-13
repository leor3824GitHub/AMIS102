using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.SetActiveThreshold;

public static class SetActiveThresholdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatch("/{id:guid}/set-active", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new SetActiveCapitalizationThresholdCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(SetActiveCapitalizationThresholdCommand))
        .WithSummary("Set a capitalization threshold circular as the currently active one")
        .RequirePermission(MasterDataModuleConstants.Permissions.CapitalizationThresholds.Manage);
}

