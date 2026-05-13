using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Data;
using AMIS.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Identity.Services;

internal sealed class UserRoleService(
    UserManager<AmisUser> userManager,
    RoleManager<AmisRole> roleManager,
    IdentityDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor) : IUserRoleService
{
    public async Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("user not found");

        await ValidateAdminRoleChangeAsync(user, userRoles);

        var assignedRoles = await ProcessRoleAssignmentsAsync(user, userRoles);

        await RaiseRolesAssignedEventAsync(user, assignedRoles, cancellationToken);

        return "User Roles Updated Successfully.";
    }

    public async Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("user not found");

        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken)
            ?? throw new NotFoundException("roles not found");

        var userRoles = new List<UserRoleDto>();
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await userManager.IsInRoleAsync(user, role.Name!)
            });
        }

        return userRoles;
    }

    private async Task ValidateAdminRoleChangeAsync(AmisUser user, List<UserRoleDto> userRoles)
    {
        bool isRemovingAdminRole = userRoles.Exists(a => !a.Enabled && a.RoleName == RoleConstants.Admin);
        if (!isRemovingAdminRole)
        {
            return;
        }

        bool userIsAdmin = await userManager.IsInRoleAsync(user, RoleConstants.Admin);
        if (!userIsAdmin)
        {
            return;
        }

        if (IsRootTenantAdmin(user))
        {
            throw new CustomException("action not permitted");
        }

        await EnsureMinimumAdminCountAsync();
    }

    private bool IsRootTenantAdmin(AmisUser user)
    {
        return user.Email == MultitenancyConstants.Root.EmailAddress
            && multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id == MultitenancyConstants.Root.Id;
    }

    private async Task EnsureMinimumAdminCountAsync()
    {
        int adminCount = (await userManager.GetUsersInRoleAsync(RoleConstants.Admin)).Count;
        if (adminCount <= 2)
        {
            throw new CustomException("tenant should have at least 2 admins.");
        }
    }

    private async Task<List<string>> ProcessRoleAssignmentsAsync(AmisUser user, List<UserRoleDto> userRoles)
    {
        var assignedRoles = new List<string>();

        foreach (var userRole in userRoles)
        {
            if (await roleManager.FindByNameAsync(userRole.RoleName!) is null)
            {
                continue;
            }

            if (userRole.Enabled)
            {
                if (!await userManager.IsInRoleAsync(user, userRole.RoleName!))
                {
                    await userManager.AddToRoleAsync(user, userRole.RoleName!);
                    assignedRoles.Add(userRole.RoleName!);
                }
            }
            else
            {
                await userManager.RemoveFromRoleAsync(user, userRole.RoleName!);
            }
        }

        return assignedRoles;
    }

    private async Task RaiseRolesAssignedEventAsync(AmisUser user, List<string> assignedRoles, CancellationToken cancellationToken)
    {
        if (assignedRoles.Count == 0)
        {
            return;
        }

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id;
        user.RecordRolesAssigned(assignedRoles, tenantId);
        await db.SaveChangesAsync(cancellationToken);
    }
}


