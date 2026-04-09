using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.MasterData.Contracts.v1.CapitalizationThresholds;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.MasterData.Features.v1.CapitalizationThresholds.CreateCapitalizationThreshold;

public static class CreateCapitalizationThresholdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateCapitalizationThresholdCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/master-data/capitalization-thresholds/{id}", id);
        })
        .WithName(nameof(CreateCapitalizationThresholdCommand))
        .WithSummary("Add a new COA capitalization threshold circular record")
        .RequirePermission(MasterDataModuleConstants.Permissions.CapitalizationThresholds.Manage);
}
