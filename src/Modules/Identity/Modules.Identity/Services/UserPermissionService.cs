using AMIS.Framework.Caching;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Constants;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Data;
using AMIS.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Identity.Services;

internal sealed class UserPermissionService(
    UserManager<AmisUser> userManager,
    RoleManager<AmisRole> roleManager,
    IdentityDbContext db,
    ICacheService cache) : IUserPermissionService
{
    public async Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        var permissions = await cache.GetOrSetAsync(
            GetPermissionCacheKey(userId),
            async () =>
            {
                var user = await userManager.FindByIdAsync(userId);

                _ = user ?? throw new UnauthorizedException();

                var userRoles = await userManager.GetRolesAsync(user);
                var permissions = new List<string>();
                foreach (var role in await roleManager.Roles
                    .Where(r => userRoles.Contains(r.Name!))
                    .ToListAsync(cancellationToken))
                {
                    permissions.AddRange(await db.RoleClaims
                        .Where(rc => rc.RoleId == role.Id && rc.ClaimType == ClaimConstants.Permission)
                        .Select(rc => rc.ClaimValue!)
                        .ToListAsync(cancellationToken));
                }
                return permissions.Distinct().ToList();
            },
            cancellationToken: cancellationToken);

        return permissions;
    }

    public static string GetPermissionCacheKey(string userId)
    {
        return $"perm:{userId}";
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(userId, cancellationToken);

        return permissions?.Contains(permission) ?? false;
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken)
    {
        return cache.RemoveItemAsync(GetPermissionCacheKey(userId), cancellationToken);
    }
}


