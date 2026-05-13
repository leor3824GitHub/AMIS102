using CommunityToolkit.Mvvm.ComponentModel;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Profile;

public sealed partial class ProfileViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    ITokenStorageService tokenStorage) : ObservableObject
{
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string? _position;
    [ObservableProperty] private string _initials = "?";
    [ObservableProperty] private bool _isLoading;

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

    public async Task LogoutAsync()
    {
        await tokenStorage.ClearAsync();
        authState.Clear();
        var loginPage = Application.Current!.Handler!.MauiContext!.Services
            .GetRequiredService<Auth.LoginPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(loginPage);
    }
}
