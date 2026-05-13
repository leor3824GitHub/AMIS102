using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Identity;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Shared.Identity.Claims;
using AMIS.Modules.Identity.Contracts.v1.Users.UpdateUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace AMIS.Modules.Identity.Features.v1.Users.UpdateUser;

public static class UpdateUserEndpoint
{
    internal static RouteHandlerBuilder MapUpdateUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/profile", async ([FromBody] UpdateUserCommand request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (user.GetUserId() is not { } userId || string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedException();
            }

            request.Id = userId;

            await mediator.Send(request, cancellationToken);
            return TypedResults.Ok();
        })
        .WithName("UpdateUserProfile")
        .WithSummary("Update user profile")
        .RequirePermission(IdentityPermissionConstants.Users.Update)
        .WithDescription("Update profile details for the authenticated user.");
    }
}

