using System.Security.Cryptography;
using System.Text;

namespace AMIS.Modules.Identity.Services;

/// <summary>
/// Single source of truth for hashing refresh tokens before they are stored or looked up.
/// Refresh tokens are never persisted in raw form; only this hash is stored on the user's session row.
/// Used by login (session creation), refresh (session rotation), and validation so every path
/// produces an identical hash for the same token.
/// </summary>
public static class RefreshTokenHasher
{
    /// <summary>
    /// Returns the full SHA-256 hash of the token as an uppercase hex string (64 chars).
    /// </summary>
    public static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
