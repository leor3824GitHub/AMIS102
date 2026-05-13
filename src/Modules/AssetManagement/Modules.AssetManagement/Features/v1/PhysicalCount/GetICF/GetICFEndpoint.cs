using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetICF;

public static class GetICFEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{sessionId:guid}/icf", async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetICFQuery(sessionId), ct)))
        .WithName(nameof(GetICFQuery))
        .WithSummary("Get the Inventory Count Form (ICF) for a physical count session (COA Circular 2020-006)")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.View);
}

