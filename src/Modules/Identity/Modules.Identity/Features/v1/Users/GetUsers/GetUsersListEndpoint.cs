using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Identity.Contracts.v1.Users.GetUsers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Identity.Features.v1.Users.GetUsers;

public static class GetUsersListEndpoint
{
    internal static RouteHandlerBuilder MapGetUsersListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/users", (CancellationToken cancellationToken, IMediator mediator) =>
            mediator.Send(new GetUsersQuery(), cancellationToken))
        .WithName("ListUsers")
        .WithSummary("List users")
        .RequirePermission(IdentityPermissionConstants.Users.View)
        .WithDescription("Retrieve a list of users for the current tenant.");
    }
}

