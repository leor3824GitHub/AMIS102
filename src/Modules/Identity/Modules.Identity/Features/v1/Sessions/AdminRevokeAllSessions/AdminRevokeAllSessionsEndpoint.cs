using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Identity.Contracts.v1.Sessions.AdminRevokeAllSessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Identity.Features.v1.Sessions.AdminRevokeAllSessions;

public static class AdminRevokeAllSessionsEndpoint
{
    internal static RouteHandlerBuilder MapAdminRevokeAllSessionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/users/{userId:guid}/sessions/revoke-all", async (Guid userId, AdminRevokeAllSessionsCommand? command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command ?? new AdminRevokeAllSessionsCommand(userId), cancellationToken);
            return TypedResults.Ok(new { RevokedCount = result });
        })
        .WithName("AdminRevokeAllSessions")
        .WithSummary("Revoke all user's sessions (Admin)")
        .RequirePermission(IdentityPermissionConstants.Sessions.RevokeAll)
        .WithDescription("Revoke all sessions for a specific user. Requires admin permission.");
    }
}

