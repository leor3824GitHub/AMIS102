using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.v1.CreateTenant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Multitenancy.Features.v1.CreateTenant;

public static class CreateTenantEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/", async (
            [FromBody] CreateTenantCommand command,
            [FromServices] IMediator mediator)
            => TypedResults.Ok(await mediator.Send(command)))
            .WithName("CreateTenant")
            .WithSummary("Create tenant")
            .RequirePermission(MultitenancyConstants.Permissions.Create)
            .WithDescription("Create a new tenant.")
            .Produces<CreateTenantCommandResponse>(StatusCodes.Status200OK);
    }
}

