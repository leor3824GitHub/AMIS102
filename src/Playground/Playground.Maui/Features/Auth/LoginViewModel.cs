using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Data.Models;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Auth;

public sealed partial class LoginViewModel(
    IApiClient apiClient,
    ITokenStorageService tokenStorage,
    IPinStorageService pinStorage,
    ICacheService cacheService,
    AuthStateService authState,
    ApiClientOptions apiOptions) : ObservableObject
{
#if DEBUG
    [ObservableProperty] private string _tenant = "root";
    [ObservableProperty] private string _email = "admin@root.com";
    [ObservableProperty] private string _password = "123Pa$$word!";
#else
    [ObservableProperty] private string _tenant = "root";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
#endif
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private string? _errorMessage;

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Tenant) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter tenant, email, and password.";
            return;
        }

        apiOptions.TenantId = Tenant.Trim();
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var tokenResponse = await apiClient.IssueTokenAsync(Email, Password, ct);
            await tokenStorage.SaveTokensAsync(tokenResponse.AccessToken, tokenResponse.RefreshToken);

            var profile = await apiClient.GetMyProfileAsync(ct);
            authState.SetUserProfile(new UserProfile(profile.Id, profile.Email, profile.FirstName, profile.LastName, profile.ImageUrl));

            var employee = await apiClient.GetMyEmployeeAsync(ct);
            authState.SetEmployee(new EmployeeInfo(employee.EmployeeId, employee.FullName, employee.Department, employee.Position));

            // Persist session metadata and identity for offline login support
            await tokenStorage.SaveLastSessionAsync(profile.Id, Tenant.Trim());
            await cacheService.SaveUserIdentityAsync(new CachedUserIdentity
            {
                UserId = profile.Id,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                TenantId = Tenant.Trim(),
                CachedAt = DateTimeOffset.UtcNow,
            });
            await cacheService.SaveEmployeeProfileAsync(new CachedEmployeeProfile
            {
                UserId = profile.Id,
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Department = employee.Department,
                Position = employee.Position,
                CachedAt = DateTimeOffset.UtcNow,
            });

            // First time login — offer PIN setup. Subsequent logins go straight to AppShell.
            var pinAlreadySet = await pinStorage.IsPinSetAsync();
            if (!pinAlreadySet)
            {
                var setPin = (SetPinPage)Application.Current!.Handler!.MauiContext!.Services
                    .GetRequiredService(typeof(SetPinPage));
                Application.Current!.MainPage = new NavigationPage(setPin);
            }
            else
            {
                Application.Current!.MainPage = new AppShell();
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Cannot reach API. Ensure Playground.Api is running and MAUI uses the correct BaseUrl.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
