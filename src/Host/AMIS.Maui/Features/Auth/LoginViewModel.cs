using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Auth;

public sealed partial class LoginViewModel(
    IApiClient apiClient,
    ITokenStorageService tokenStorage,
    AuthStateService authState,
    ApiClientOptions apiOptions) : ObservableObject
{
    [ObservableProperty] public partial string Tenant { get; set; } = Preferences.Default.Get(ApiClientOptions.TenantPreferenceKey, "root");
    [ObservableProperty] public partial string Email { get; set; } = "admin@root.com";
    [ObservableProperty] public partial string Password { get; set; } = "123Pa$$word!";
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsPasswordVisible { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

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

            // Persist only after a successful login so a typo never poisons the next session resume.
            Preferences.Default.Set(ApiClientOptions.TenantPreferenceKey, apiOptions.TenantId);

            Application.Current!.MainPage = new AppShell();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Cannot reach API. Ensure AMIS.Api is running and MAUI uses the correct BaseUrl.";
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
