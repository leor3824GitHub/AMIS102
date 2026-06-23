using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace AMIS.Blazor.Services;

/// <summary>Last-used Page Setup values for a QuestPDF report, persisted per report type.</summary>
/// <remarks>
/// <paramref name="MarginMm"/> is the uniform page margin in millimetres. New fields are
/// optional so values persisted before they existed still deserialize.
/// </remarks>
internal sealed record QuestPdfSetup(
    string PaperSize,
    string Orientation,
    double MarginMm = 15d);

/// <summary>
/// Remembers the user's last-used QuestPDF Page Setup (paper size, orientation, margin)
/// per report type, persisted in encrypted browser localStorage. The storage key is scoped
/// by tenant + user so two users on the same device never share settings. Device-local: it
/// does not roam to other browsers/PCs.
/// </summary>
internal interface IQuestPdfSetupPreferences
{
    Task<QuestPdfSetup?> GetAsync(string reportKey);
    Task SaveAsync(string reportKey, QuestPdfSetup setup);
}

internal sealed class QuestPdfSetupPreferences(
    ProtectedLocalStorage storage,
    IUserProfileState profile,
    AuthenticationStateProvider authStateProvider) : IQuestPdfSetupPreferences
{
    private const string KeyPrefix = "amis-questpdf-setup";

    public async Task<QuestPdfSetup?> GetAsync(string reportKey)
    {
        try
        {
            var result = await storage.GetAsync<QuestPdfSetup>(await BuildKeyAsync(reportKey));
            return result.Success ? result.Value : null;
        }
        catch
        {
            // Stale/undecryptable value or interop unavailable — fall back to defaults.
            return null;
        }
    }

    public async Task SaveAsync(string reportKey, QuestPdfSetup setup)
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
