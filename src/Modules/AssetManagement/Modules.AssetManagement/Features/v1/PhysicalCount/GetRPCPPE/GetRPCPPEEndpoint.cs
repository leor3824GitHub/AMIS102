using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCPPE;

public static class GetRPCPPEEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{sessionId:guid}/rpcppe", async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetRPCPPEQuery(sessionId), ct)))
        .WithName(nameof(GetRPCPPEQuery))
        .WithSummary("Get the Report on the Physical Count of PPE (RPCPPE) — COA Circular 2020-006, Annex A")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.View);
}
