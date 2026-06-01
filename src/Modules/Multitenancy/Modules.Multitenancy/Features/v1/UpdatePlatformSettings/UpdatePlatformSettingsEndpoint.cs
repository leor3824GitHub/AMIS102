using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdatePlatformSettings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdatePlatformSettings;

public static class UpdatePlatformSettingsEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/settings", async (PlatformSettingsDto settings, IMediator mediator, CancellationToken cancellationToken) =>
            {
                await mediator.Send(new UpdatePlatformSettingsCommand(settings), cancellationToken);
                return TypedResults.NoContent();
            })
            .WithName("Multitenancy_UpdatePlatformSettings")
            .WithSummary("Update global platform settings")
            .WithDescription("Update the global, platform-wide session and quota settings. Root tenant only.")
            .RequirePermission(MultitenancyConstants.Permissions.UpdateSettings)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }
}
