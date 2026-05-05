using CommunityToolkit.Mvvm.ComponentModel;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Profile;

public sealed partial class ProfileViewModel(
    IApiClient apiClient,
    AuthStateService authState,
    ITokenStorageService tokenStorage,
    ICacheService cacheService) : ObservableObject
{
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string? _department;
    [ObservableProperty] private string? _position;
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LogoutAsync()
    {
        await tokenStorage.ClearAsync();
        await cacheService.ClearAllAsync();
        authState.Clear();
        // Replace window root with a fresh login page
        var loginPage = Application.Current!.Handler!.MauiContext!.Services
            .GetRequiredService<Auth.LoginPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(loginPage);
    }
}
