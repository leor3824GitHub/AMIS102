using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace AMIS.Blazor.Services;

/// <summary>
/// Remembers the user's last-used "Date Assumed Accountability" for the Physical Count report,
/// persisted in encrypted browser localStorage. The storage key is scoped by tenant + user so two
/// users on the same device never share the value. Device-local: it does not roam to other browsers/PCs.
/// </summary>
internal interface IAssumedAccountabilityDatePreference
{
    Task<DateTime?> GetAsync();
    Task SaveAsync(DateTime? date);
}

internal sealed class AssumedAccountabilityDatePreference(
    ProtectedLocalStorage storage,
    IUserProfileState profile,
    AuthenticationStateProvider authStateProvider) : IAssumedAccountabilityDatePreference
{
    private const string KeyPrefix = "amis-assumed-accountability-date";

    public async Task<DateTime?> GetAsync()
    {
        try
        {
            var result = await storage.GetAsync<DateTime?>(await BuildKeyAsync());
            return result.Success ? result.Value : null;
        }
        catch
        {
            // Stale/undecryptable value or interop unavailable — fall back to no value.
            return null;
        }
    }

    public async Task SaveAsync(DateTime? date)
    {
        try
        {
            await storage.SetAsync(await BuildKeyAsync(), date);
        }
        catch
        {
            // Persisting the preference is best-effort; never block report generation on storage failure.
        }
    }

    private async Task<string> BuildKeyAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        var tenant = state.User.FindFirst("tenant")?.Value ?? "root";
        var user = profile.UserEmail ?? state.User.Identity?.Name ?? "anon";
        return $"{KeyPrefix}:{tenant}:{user}";
    }
}
