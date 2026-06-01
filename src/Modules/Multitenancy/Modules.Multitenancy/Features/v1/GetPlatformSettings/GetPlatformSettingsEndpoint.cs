using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.GetPlatformSettings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Multitenancy.Features.v1.GetPlatformSettings;

public static class GetPlatformSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/settings", async (IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetPlatformSettingsQuery(), cancellationToken)))
            .WithName("Multitenancy_GetPlatformSettings")
            .WithSummary("Get global platform settings")
            .WithDescription("Retrieve the global, platform-wide session and quota settings that apply to every tenant.")
            .RequirePermission(MultitenancyConstants.Permissions.ViewSettings)
            .Produces<PlatformSettingsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
