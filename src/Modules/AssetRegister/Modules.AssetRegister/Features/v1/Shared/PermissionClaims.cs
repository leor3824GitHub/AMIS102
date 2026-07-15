using AMIS.Framework.Core.Context;
using AMIS.Framework.Shared.Constants;

namespace AMIS.Modules.AssetRegister.Features.v1.Shared;

/// <summary>
/// Reads the authenticated user's permission claims directly, for handler-level authorization decisions
/// that go beyond a single endpoint's <c>.RequirePermission(...)</c> gate — e.g. "privileged users see all
/// return receipts, everyone else only their own", or "only the requester or a custodian may withdraw a
/// return". Effective permissions are emitted as <see cref="CustomClaims.Permission"/> claims at login.
/// </summary>
internal static class PermissionClaims
{
    /// <summary>True when the current user holds at least one of the given permission strings.</summary>
    public static bool HasAny(ICurrentUser currentUser, params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(permissions);

        var claims = currentUser.GetUserClaims();
        if (claims is null || permissions.Length == 0)
            return false;

        var wanted = permissions.ToHashSet(StringComparer.Ordinal);
        return claims.Any(c => c.Type == CustomClaims.Permission && wanted.Contains(c.Value));
    }
}
