using AMIS.Framework.Shared.Identity.Claims;
using AMIS.Modules.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AMIS.Modules.Identity.Authorization;

public sealed class RequiredPermissionAuthorizationHandler(IUserService userService) : AuthorizationHandler<PermissionAuthorizationRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var httpContext = context.Resource as HttpContext;
        var endpoint = context.Resource switch
        {
            HttpContext ctx => ctx.GetEndpoint(),
            Endpoint ep => ep,
            _ => null,
        };

        var requiredPermissions = endpoint?.Metadata.GetMetadata<IRequiredPermissionMetadata>()?.RequiredPermissions;
        if (requiredPermissions == null)
        {
            // there are no permission requirements set by the endpoint
            // hence, authorize requests
            context.Succeed(requirement);
            return;
        }

        var cancellationToken = httpContext?.RequestAborted ?? CancellationToken.None;
        if (context.User?.GetUserId() is not { } userId)
        {
            return;
        }

        // The user must hold every permission the endpoint declares, not just the first.
        foreach (var permission in requiredPermissions)
        {
            if (!await userService.HasPermissionAsync(userId, permission, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        context.Succeed(requirement);
    }
}

