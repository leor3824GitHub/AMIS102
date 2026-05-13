using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Identity.Contracts.v1.Users.GetUserGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Identity.Features.v1.Users.GetUserGroups;

public static class GetUserGroupsEndpoint
{
    public static RouteHandlerBuilder MapGetUserGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users/{userId}/groups", (string userId, IMediator mediator, CancellationToken cancellationToken) =>
            mediator.Send(new GetUserGroupsQuery(userId), cancellationToken))
        .WithName("GetUserGroups")
        .WithSummary("Get groups for a user")
        .RequirePermission(IdentityPermissionConstants.Groups.View)
        .WithDescription("Retrieve all groups that a specific user belongs to.");
    }
}

