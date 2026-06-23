using CommunityToolkit.Mvvm.ComponentModel;
using Playground.Maui.Data;
using Playground.Maui.Services;
using System.Reflection;

namespace Playground.Maui.Features.Profile;

public sealed partial class ProfileViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    ITokenStorageService tokenStorage,
    IPhysicalCountSyncService syncService,
    LocalDb localDb) : ObservableObject
{
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string? _position;
    [ObservableProperty] private string _initials = "?";
    [ObservableProperty] private bool _isLoading;

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
        try
        {
            var profile = await apiClient.GetMyProfileAsync(ct);
            Email = profile.Email;
            FullName = $"{profile.FirstName} {profile.LastName}".Trim();

            if (authState.Employee is not null)
            {
                Department = authState.Employee.Department;
                Position = authState.Employee.Position;
                FullName = authState.Employee.FullName;
            }

            Initials = ComputeInitials(FullName);
        }
        finally
        {
            IsLoading = false;
        }
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
        var loginPage = Application.Current!.Handler!.MauiContext!.Services
            .GetRequiredService<Auth.LoginPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(loginPage);
    }
}
