using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;

namespace Playground.Maui.Features.Auth;

public sealed partial class LoginViewModel(
    IApiClient apiClient,
    ITokenStorageService tokenStorage,
    AuthStateService authState,
    ApiClientOptions apiOptions) : ObservableObject
{
#if DEBUG
    // Development defaults for easy testing on MAUI
    [ObservableProperty] private string _tenant = "root";
    [ObservableProperty] private string _email = "admin@root.com";
    [ObservableProperty] private string _password = "123Pa$$word!";
#else
    [ObservableProperty] private string _tenant = "root";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
#endif
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

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

            Application.Current!.MainPage = new AppShell();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Login failed. Check your credentials and try again.";
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
