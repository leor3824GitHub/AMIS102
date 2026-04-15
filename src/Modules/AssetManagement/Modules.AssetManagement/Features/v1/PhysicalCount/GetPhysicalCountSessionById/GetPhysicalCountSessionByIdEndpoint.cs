using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.GetPhysicalCountSessionById;

public static class GetPhysicalCountSessionByIdEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetPhysicalCountSessionByIdQuery(id), ct)))
        .WithName(nameof(GetPhysicalCountSessionByIdQuery))
        .WithSummary("Get a physical count session with all its checklist entries")
        .RequirePermission(AssetManagementModuleConstants.Permissions.PhysicalCount.View);
}
