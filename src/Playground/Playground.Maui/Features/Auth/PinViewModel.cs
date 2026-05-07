using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Auth;

public sealed partial class PinViewModel(
    ITokenStorageService tokenStorage,
    IPinStorageService pinStorage,
    ICacheService cacheService,
    AuthStateService authState,
    ApiClientOptions apiOptions) : ObservableObject
{
    private const int MaxAttempts = 3;
    private int _attempts;

    [ObservableProperty] private string _pin = "";
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        var (userId, tenantId) = await tokenStorage.GetLastSessionAsync();
        if (userId is null) { NavigateToLogin(); return; }

        var employee = await cacheService.GetEmployeeProfileAsync(userId);
        var identity = await cacheService.GetUserIdentityAsync(userId);
        if (employee is null || identity is null) { NavigateToLogin(); return; }

        DisplayName = !string.IsNullOrWhiteSpace(employee.FullName)
            ? employee.FullName
            : identity.Email;

        if (!string.IsNullOrEmpty(tenantId))
            apiOptions.TenantId = tenantId;
    }

    [RelayCommand]
    private async Task UnlockAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(Pin)) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var (userId, _) = await tokenStorage.GetLastSessionAsync();
            if (userId is null) { NavigateToLogin(); return; }

            var valid = await pinStorage.VerifyPinAsync(Pin, userId);
            if (!valid)
            {
                Pin = "";
                _attempts++;
                if (_attempts >= MaxAttempts)
                {
                    NavigateToLogin();
                    return;
                }
                ErrorMessage = $"Incorrect PIN. {MaxAttempts - _attempts} attempt(s) remaining.";
                return;
            }

            // Restore full auth state from cache
            var identity = await cacheService.GetUserIdentityAsync(userId);
            var employee = await cacheService.GetEmployeeProfileAsync(userId);
            if (identity is null || employee is null) { NavigateToLogin(); return; }

            authState.SetUserProfile(new UserProfile(
                identity.UserId, identity.Email, identity.FirstName, identity.LastName, null));
            authState.SetEmployee(new EmployeeInfo(
                employee.EmployeeId, employee.FullName, employee.Department, employee.Position));

            Application.Current!.MainPage = new AppShell();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void UsePassword() => NavigateToLogin();

    private static void NavigateToLogin()
    {
        var loginPage = (LoginPage)Application.Current!.Handler!.MauiContext!.Services
            .GetRequiredService(typeof(LoginPage));
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
