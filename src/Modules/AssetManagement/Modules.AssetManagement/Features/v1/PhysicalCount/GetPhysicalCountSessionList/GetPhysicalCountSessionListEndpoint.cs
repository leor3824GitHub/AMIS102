using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionList;

public static class GetPhysicalCountSessionListEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (
            [AsParameters] GetPhysicalCountSessionListQuery query,
            IMediator mediator,
            CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetPhysicalCountSessionListQuery))
        .WithSummary("List physical count sessions with progress summary")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.View);
}

