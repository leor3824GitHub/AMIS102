using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetRPCSEMEX;

public static class GetRPCSEMEXEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{sessionId:guid}/rpcsemex", async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetRPCSEMEXQuery(sessionId), ct)))
        .WithName(nameof(GetRPCSEMEXQuery))
        .WithSummary("Get the Report on the Physical Count of Semi-Expendable Properties (RPCSEMEX) â€” COA Circular 2020-006")
        .RequirePermission(AssetManagementPermissions.PhysicalCount.View);
}

