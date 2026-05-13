using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.MasterData.Features.v1.CapitalizationThresholds.UpdateCapitalizationThreshold;

public static class UpdateCapitalizationThresholdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateCapitalizationThresholdCommand command, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(command with { Id = id }, ct)))
        .WithName(nameof(UpdateCapitalizationThresholdCommand))
        .WithSummary("Update a capitalization threshold circular record")
        .RequirePermission(MasterDataModuleConstants.Permissions.CapitalizationThresholds.Manage);
}

