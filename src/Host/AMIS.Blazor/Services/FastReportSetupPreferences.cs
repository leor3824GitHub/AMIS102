using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace AMIS.Blazor.Services;

/// <summary>Last-used Page Setup values for a Fast Report, persisted per report type.</summary>
/// <remarks>
/// <paramref name="DataOnly"/> and the offsets apply only to reports that support overlay
/// printing onto pre-printed accountable forms (PPERR / PPEIR). Offsets are in millimetres.
/// New fields are optional so values persisted before they existed still deserialize.
/// </remarks>
internal sealed record FastReportSetup(
    string PaperSize,
    string Orientation,
    int Copies,
    int MinRows,
    bool DataOnly = false,
    double OffsetX = 0,
    double OffsetY = 0);

/// <summary>
/// Remembers the user's last-used Fast Report Page Setup (paper size, orientation,
/// copies, rows) per report type, persisted in encrypted browser localStorage. The
/// storage key is scoped by tenant + user so two users on the same device never share
/// settings. Device-local: it does not roam to other browsers/PCs.
/// </summary>
internal interface IFastReportSetupPreferences
{
    Task<FastReportSetup?> GetAsync(string reportKey);
    Task SaveAsync(string reportKey, FastReportSetup setup);
}

internal sealed class FastReportSetupPreferences(
    ProtectedLocalStorage storage,
    IUserProfileState profile,
    AuthenticationStateProvider authStateProvider) : IFastReportSetupPreferences
{
    private const string KeyPrefix = "amis-fastreport-setup";

    public async Task<FastReportSetup?> GetAsync(string reportKey)
    {
        try
        {
            var result = await storage.GetAsync<FastReportSetup>(await BuildKeyAsync(reportKey));
            return result.Success ? result.Value : null;
        }
        catch
        {
            // Stale/undecryptable value or interop unavailable — fall back to defaults.
            return null;
        }
    }

    public async Task SaveAsync(string reportKey, FastReportSetup setup)
    {
        try
        {
            await storage.SetAsync(await BuildKeyAsync(reportKey), setup);
        }
        catch
        {
            // Persisting the preference is best-effort; never block printing on storage failure.
        }
    }

    private async Task<string> BuildKeyAsync(string reportKey)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        var tenant = state.User.FindFirst("tenant")?.Value ?? "root";
        var user = profile.UserEmail ?? state.User.Identity?.Name ?? "anon";
        return $"{KeyPrefix}:{tenant}:{user}:{reportKey}";
    }
}
