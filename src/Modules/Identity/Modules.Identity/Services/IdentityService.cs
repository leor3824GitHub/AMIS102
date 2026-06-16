using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AMIS.Modules.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AmisUser> _userManager;
    private readonly SignInManager<AmisUser> _signInManager;
    private readonly ILogger<IdentityService> _logger;
    private readonly IMultiTenantContextAccessor<AppTenantInfo>? _multiTenantContextAccessor;
    private readonly IGroupRoleService _groupRoleService;
    private readonly ISessionService _sessionService;

    public IdentityService(
        UserManager<AmisUser> userManager,
        SignInManager<AmisUser> signInManager,
        IMultiTenantContextAccessor<AppTenantInfo>? multiTenantContextAccessor,
        ILogger<IdentityService> logger,
        IGroupRoleService groupRoleService,
        ISessionService sessionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _logger = logger;
        _groupRoleService = groupRoleService;
        _sessionService = sessionService;
    }

    public async Task<(string Subject, IEnumerable<Claim> Claims)?>
        ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var tenant = GetValidatedTenant();
        var user = await FindAndValidateUserByCredentialsAsync(email, password);

        ValidateUserStatus(user);
        ValidateTenantStatus(tenant);

        var claims = await BuildUserClaimsAsync(user, tenant.Id, ct);
        return (user.Id, claims);
    }

    public async Task<(string Subject, IEnumerable<Claim> Claims)?>
        ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        AppTenantInfo tenant;
        try
        {
            tenant = GetValidatedTenant();
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogDebug(ex, "Refresh token validation failed: tenant context is missing or invalid");
            return null;
        }

        // Sessions are the source of truth: each device/login owns its own session row, so two
        // platforms no longer clobber each other through a single user column.
        var refreshTokenHash = RefreshTokenHasher.Hash(refreshToken);
        var userId = await _sessionService.GetActiveUserIdByRefreshTokenAsync(refreshTokenHash, ct);
        if (userId is null)
        {
            _logger.LogDebug("No active session found for the supplied refresh token in tenant {TenantId}", tenant.Id);
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogDebug("Session {UserId} references a user that no longer exists in tenant {TenantId}", userId, tenant.Id);
            return null;
        }

        try
        {
            ValidateUserStatus(user);
            ValidateTenantStatus(tenant);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogDebug(
                ex,
                "Refresh token validation failed for user {UserId} in tenant {TenantId}: {Reason}",
                user.Id,
                tenant.Id,
                ex.Message);
            return null;
        }

        var claims = await BuildUserClaimsAsync(user, tenant.Id, ct);
        return (user.Id, claims);
    }

    private AppTenantInfo GetValidatedTenant()
    {
        var tenant = _multiTenantContextAccessor!.MultiTenantContext.TenantInfo
            ?? throw new UnauthorizedException();

        if (string.IsNullOrWhiteSpace(tenant.Id))
        {
            throw new UnauthorizedException();
        }

        return tenant;
    }

    private async Task<AmisUser> FindAndValidateUserByCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim().Normalize());
        if (user is null)
        {
            // Burn a hash so missing and existing accounts take comparable time (timing-based enumeration).
            new PasswordHasher<AmisUser>().HashPassword(new AmisUser(), password);
            throw new UnauthorizedException();
        }

        // lockoutOnFailure increments AccessFailedCount and enforces the configured lockout policy.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("Login blocked: account {UserId} is locked out", user.Id);
            }

            throw new UnauthorizedException();
        }

        return user;
    }

    private static void ValidateUserStatus(AmisUser user)
    {
        if (!user.IsActive)
        {
            throw new UnauthorizedException("user is deactivated");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedException("email not confirmed");
        }
    }

    private static void ValidateTenantStatus(AppTenantInfo tenant)
    {
        if (tenant.Id == MultitenancyConstants.Root.Id)
        {
            return;
        }

        if (!tenant.IsActive)
        {
            throw new UnauthorizedException($"tenant {tenant.Id} is deactivated");
        }

        if (DateTime.UtcNow > tenant.ValidUpto)
        {
            throw new UnauthorizedException($"tenant {tenant.Id} validity has expired");
        }
    }

    private async Task<List<Claim>> BuildUserClaimsAsync(AmisUser user, string tenantId, CancellationToken ct)
    {
        var claims = CreateBasicClaims(user, tenantId);
        await AddRoleClaimsAsync(claims, user, ct);
        return claims;
    }

    private static List<Claim> CreateBasicClaims(AmisUser user, string tenantId) =>
    [
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email, user.Email!),
        new(ClaimTypes.Name, user.FirstName ?? string.Empty),
        new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
        new(ClaimConstants.Fullname, $"{user.FirstName} {user.LastName}"),
        new(ClaimTypes.Surname, user.LastName ?? string.Empty),
        new(ClaimConstants.Tenant, tenantId),
        new(ClaimConstants.ImageUrl, user.ImageUrl?.ToString() ?? string.Empty)
    ];

    private async Task AddRoleClaimsAsync(List<Claim> claims, AmisUser user, CancellationToken ct)
    {
        var directRoles = await _userManager.GetRolesAsync(user);
        var groupRoles = await _groupRoleService.GetUserGroupRolesAsync(user.Id, ct);

        var allRoles = directRoles.Union(groupRoles).Distinct();
        claims.AddRange(allRoles.Select(r => new Claim(ClaimTypes.Role, r)));
    }
}


