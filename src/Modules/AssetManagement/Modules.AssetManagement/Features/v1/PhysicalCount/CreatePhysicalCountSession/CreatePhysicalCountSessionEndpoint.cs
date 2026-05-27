using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.AssetManagement.Contracts.Permissions;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;

public static class CreatePhysicalCountSessionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async ([FromBody] CreatePhysicalCountSessionCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/asset-management/physical-count/{result.SessionId}", result);
        })
        .WithName(nameof(CreatePhysicalCountSessionCommand))
        .WithSummary("Create a Physical Count Session and generate the asset checklist (ICF preparation)")
        .RequirePermission(AssetManagementPermissions.PhysicalCount.Create);
}

