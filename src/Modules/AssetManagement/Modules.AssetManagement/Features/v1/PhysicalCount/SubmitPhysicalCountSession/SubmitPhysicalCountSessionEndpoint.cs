using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.SubmitPhysicalCountSession;

public static class SubmitPhysicalCountSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{sessionId:guid}/submit", async (
            Guid sessionId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new SubmitPhysicalCountSessionCommand(sessionId), ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(SubmitPhysicalCountSessionCommand))
        .WithSummary("Submit and lock a Physical Count Session; unverified entries are auto-marked as Not Found")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.Update);
}
