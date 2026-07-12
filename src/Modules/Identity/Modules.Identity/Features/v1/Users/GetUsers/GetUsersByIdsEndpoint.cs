using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Identity.Contracts.v1.Users.GetUsers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Identity.Features.v1.Users.GetUsers;

public static class GetUsersByIdsEndpoint
{
    internal static RouteHandlerBuilder MapGetUsersByIdsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/users/by-ids",
                (   [FromBody] IReadOnlyCollection<string> userIds,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(new GetUsersByIdsQuery(userIds ?? []), cancellationToken))
            // Contracts-shared query type — prefix the endpoint name so it can't collide across modules.
            .WithName("Identity_GetUsersByIds")
            .WithSummary("Get users for a set of ids (batch)")
            .WithDescription("Resolves many users by id in one call — replaces fetching the whole user list just to display a few linked accounts.")
            .RequirePermission(IdentityPermissionConstants.Users.View);
    }
}
