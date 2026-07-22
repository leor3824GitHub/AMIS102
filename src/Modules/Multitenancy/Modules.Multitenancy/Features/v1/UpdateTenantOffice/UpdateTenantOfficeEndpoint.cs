using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdateTenantOffice;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdateTenantOffice;

public static class UpdateTenantOfficeEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/{tenantId}/office", async (
            string tenantId,
            [FromBody] UpdateTenantOfficeCommand command,
            [FromServices] IMediator mediator)
            => TypedResults.Ok(await mediator.Send(command with { TenantId = tenantId })))
            .WithName("Multitenancy_UpdateTenantOffice")
            .WithSummary("Link a tenant to its office")
            .RequirePermission(MultitenancyConstants.Permissions.Update)
            .WithDescription("Points a tenant at the MasterData office it represents, so recipients can be resolved to a destination agency for inter-agency transfers.")
            .Produces<UpdateTenantOfficeCommandResponse>(StatusCodes.Status200OK);
    }
}
