using CommunityToolkit.Mvvm.ComponentModel;
using AMIS.Maui.Data;
using AMIS.Maui.Services;
using System.Reflection;

namespace AMIS.Maui.Features.Profile;

public sealed partial class ProfileViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    ITokenStorageService tokenStorage,
    IPhysicalCountSyncService syncService,
    LocalDb localDb,
    ChatUnreadService chatUnread) : ObservableObject
{
    [ObservableProperty] public partial string FullName { get; set; } = "";
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string? Department { get; set; }
    [ObservableProperty] public partial string? Position { get; set; }
    [ObservableProperty] public partial string Initials { get; set; } = "?";
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    // Live build version, read from the AssemblyInformationalVersion the build stamps in
    // (e.g. "1.0.0+build.142" -> "Version 1.0.0 (build 142)").
    public string AppVersion { get; } = BuildVersionString();

    private static string BuildVersionString()
    {
        var info = typeof(ProfileViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(info))
            return "Version 1.0";

        const string marker = "+build.";
        var idx = info.IndexOf(marker, StringComparison.Ordinal);
        return idx >= 0
            ? $"Version {info[..idx]} (build {info[(idx + marker.Length)..]})"
            : $"Version {info}";
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;

        // Seed from the in-memory auth state first (no network). This guarantees the page shows
        // the signed-in identity even when the profile API call fails — and, critically, means a
        // failed call degrades to an error banner instead of an unhandled exception. An uncaught
        // throw here would surface on the UI thread via ProfilePage.OnAppearing (async void) and
        // hard-crash the Windows app, ending the Aspire process.
        ApplyKnownIdentity();

        try
        {
            var profile = await apiClient.GetMyProfileAsync(ct);
            Email = profile.Email;

            // Only let the profile name win when there's no employee record to defer to.
            if (authState.Employee is null)
            {
                FullName = $"{profile.FirstName} {profile.LastName}".Trim();
                Initials = ComputeInitials(FullName);
            }
        }
        catch (OperationCanceledException)
        {
            // Navigated away mid-load — not an error.
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ErrorMessage = "You're offline. Showing saved profile details.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not refresh your profile.";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Something went wrong loading your profile.";
            System.Diagnostics.Debug.WriteLine($"[Profile] Load failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyKnownIdentity()
    {
        if (authState.Employee is { } emp)
        {
            FullName = emp.FullName;
            Department = emp.Department;
            Position = emp.Position;
        }

        if (authState.UserProfile is { } up)
        {
            Email = up.Email;
            if (string.IsNullOrWhiteSpace(FullName))
                FullName = $"{up.FirstName} {up.LastName}".Trim();
        }

        Initials = ComputeInitials(FullName);
    }

    private static string ComputeInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => char.ToUpperInvariant(parts[0][0]).ToString(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };
    }

    /// <summary>
    /// Number of unsynced count entries still queued locally. Used by the Sign Out
    /// confirmation to warn the user before their cached data is wiped on logout.
    /// </summary>
    public Task<int> GetPendingSyncCountAsync() => syncService.GetPendingCountAsync();

    public async Task LogoutAsync()
    {
        await tokenStorage.ClearAsync();
        await localDb.ClearAllAsync();
        authState.Clear();
        chatUnread.Clear(); // read markers are per-device; don't hand them to the next user
        var loginPage = Application.Current!.Handler!.MauiContext!.Services
            .GetRequiredService<Auth.LoginPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(loginPage);
    }
}
